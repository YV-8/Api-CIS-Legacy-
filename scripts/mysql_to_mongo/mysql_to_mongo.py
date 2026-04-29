#!/usr/bin/env python3
"""
mysql_to_mongo.py
------------------
Herramienta de migración de usuarios de MySQL → MongoDB para el microservicio
user-management-ms. Soporta verificación de conectividad, migración con
rollback automático y rollback manual de la última sesión.

Modelo migrado
--------------
  MySQL  (tabla users)       →   MongoDB (colección users)
  -----------------------------------------------------------
  id      VARCHAR(36) UUID       _id / id   String
  name    VARCHAR(200)           name       String
  login   VARCHAR(20)  unique    login      String  (indexed unique)
  password VARCHAR(100)          password   String
  role    ENUM(ADMIN,OWNER,USER) role       String

Uso
---
  python mysql_to_mongo.py check      # verifica conectividad con MySQL y MongoDB
  python mysql_to_mongo.py migrate    # migra con rollback automático ante fallos
  python mysql_to_mongo.py rollback   # rollback manual de la última sesión
"""

import argparse
import json
import logging
import os
import sys
from datetime import datetime, timezone
from pathlib import Path

import certifi
import pymysql
import pymysql.cursors
from dotenv import load_dotenv
from pymongo import MongoClient, UpdateOne
from pymongo.errors import BulkWriteError, ConnectionFailure, PyMongoError

# ─── Logging ─────────────────────────────────────────────────────────────────

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)-8s  %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S",
)
log = logging.getLogger(__name__)

# ─── Rutas ───────────────────────────────────────────────────────────────────

_ROOT = Path(__file__).resolve().parent.parent.parent
_STATE_FILE = Path(__file__).resolve().parent / "migration_state.json"

load_dotenv(_ROOT / ".env", override=False)


def _env(key: str, default: str | None = None) -> str | None:
    return os.environ.get(key, default)


# ─── Argumentos CLI ───────────────────────────────────────────────────────────

def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(
        description="Migra usuarios de MySQL → MongoDB para user-management-ms",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Subcomandos disponibles:
  check     Verifica la conectividad con MySQL y MongoDB
  migrate   Ejecuta la migración con rollback automático ante fallos
  rollback  Deshace la última migración eliminando solo los documentos insertados
        """,
    )

    sub = p.add_subparsers(dest="command", metavar="SUBCOMANDO")
    sub.required = True

    # ── Argumentos comunes de conexión ────────────────────────────────────────
    def add_connection_args(parser):
        mg = parser.add_argument_group("MySQL (origen)")
        mg.add_argument("--mysql-host", default=_env("MYSQL_HOST", "localhost"))
        mg.add_argument("--mysql-port", type=int, default=int(_env("MYSQL_PORT", "3307")))
        mg.add_argument("--mysql-db",   default=_env("DB_NAME", "sd3"))
        mg.add_argument("--mysql-user", default=_env("DB_USERNAME"))
        mg.add_argument("--mysql-password", default=_env("DB_PASSWORD"))

        mo = parser.add_argument_group("MongoDB (destino)")
        mo.add_argument(
            "--mongo-uri",
            default=_env("MONGODB_URI", "mongodb://localhost:27017"),
        )
        mo.add_argument(
            "--mongo-db",
            default=_env("MONGO_DB_NAME", _env("DB_NAME", "sd3")),
        )
        mo.add_argument("--mongo-collection", default="users")
        parser.add_argument("--verbose", "-v", action="store_true")

    # ── check ─────────────────────────────────────────────────────────────────
    p_check = sub.add_parser("check", help="Verifica la conectividad con MySQL y MongoDB")
    add_connection_args(p_check)

    # ── migrate ───────────────────────────────────────────────────────────────
    p_migrate = sub.add_parser("migrate", help="Ejecuta la migración")
    add_connection_args(p_migrate)
    p_migrate.add_argument(
        "--dry-run",
        action="store_true",
        help="Lee de MySQL pero NO escribe en MongoDB.",
    )
    p_migrate.add_argument(
        "--batch-size",
        type=int,
        default=500,
        help="Documentos por operación bulk (default: 500).",
    )

    # ── rollback ──────────────────────────────────────────────────────────────
    p_rollback = sub.add_parser(
        "rollback",
        help="Deshace la última migración (solo elimina documentos insertados por el script)",
    )
    mo = p_rollback.add_argument_group("MongoDB")
    mo.add_argument("--mongo-uri", default=_env("MONGODB_URI", "mongodb://localhost:27017"))
    mo.add_argument("--mongo-db",  default=_env("MONGO_DB_NAME", _env("DB_NAME", "sd3")))
    mo.add_argument("--mongo-collection", default="users")
    p_rollback.add_argument("--verbose", "-v", action="store_true")

    return p.parse_args()


# ─── Helpers MySQL ────────────────────────────────────────────────────────────

def connect_mysql(args: argparse.Namespace) -> pymysql.Connection:
    if not args.mysql_user:
        log.error("Falta usuario MySQL. Configurá DB_USERNAME en el .env o usá --mysql-user.")
        sys.exit(1)
    if not args.mysql_password:
        log.error("Falta contraseña MySQL. Configurá DB_PASSWORD en el .env o usá --mysql-password.")
        sys.exit(1)

    log.info("Conectando a MySQL  %s:%s  /  base: %s", args.mysql_host, args.mysql_port, args.mysql_db)
    try:
        conn = pymysql.connect(
            host=args.mysql_host,
            port=args.mysql_port,
            user=args.mysql_user,
            password=args.mysql_password,
            database=args.mysql_db,
            charset="utf8mb4",
            cursorclass=pymysql.cursors.DictCursor,
            connect_timeout=10,
        )
        log.info("Conexión MySQL  ✓")
        return conn
    except pymysql.Error as e:
        log.error("No se pudo conectar a MySQL: %s", e)
        sys.exit(1)


def fetch_users(conn: pymysql.Connection) -> list[dict]:
    with conn.cursor() as cur:
        cur.execute("SELECT id, name, login, password, role FROM users")
        rows = cur.fetchall()
    log.info("Usuarios encontrados en MySQL: %d", len(rows))
    return rows


# ─── Helpers MongoDB ──────────────────────────────────────────────────────────

def connect_mongo(args: argparse.Namespace) -> MongoClient:
    log.info("Conectando a MongoDB  %s", _safe_uri(args.mongo_uri))
    try:
        client = MongoClient(
            args.mongo_uri,
            serverSelectionTimeoutMS=10_000,
            tls=True,
            tlsCAFile=certifi.where(),
            tlsAllowInvalidCertificates=True,
        )
        client.admin.command("ping")
        log.info("Conexión MongoDB  ✓")
        return client
    except ConnectionFailure as e:
        log.error("No se pudo conectar a MongoDB: %s", e)
        sys.exit(1)


def _safe_uri(uri: str) -> str:
    try:
        from urllib.parse import urlparse, urlunparse
        parsed = urlparse(uri)
        if parsed.password:
            netloc = f"{parsed.username}:****@{parsed.hostname}"
            if parsed.port:
                netloc += f":{parsed.port}"
            return urlunparse(parsed._replace(netloc=netloc))
    except Exception:
        pass
    return uri


def mysql_row_to_document(row: dict) -> dict:
    return {
        "_id":      row["id"],
        "id":       row["id"],
        "name":     row["name"],
        "login":    row["login"],
        "password": row["password"],
        "role":     row["role"],
    }


def snapshot_existing_ids(collection) -> set[str]:
    ids = {doc["_id"] for doc in collection.find({}, {"_id": 1})}
    log.info("Documentos pre-existentes en la colección: %d", len(ids))
    return ids


def upsert_batch(collection, documents: list[dict]) -> tuple[int, int, list[str]]:
    operations = [
        UpdateOne({"_id": doc["_id"]}, {"$set": doc}, upsert=True)
        for doc in documents
    ]
    try:
        result = collection.bulk_write(operations, ordered=False)
        written = result.upserted_count + result.modified_count
        return written, 0, [doc["_id"] for doc in documents]
    except BulkWriteError as bwe:
        errors = bwe.details.get("writeErrors", [])
        error_indices = {e["index"] for e in errors}
        written = bwe.details.get("nUpserted", 0) + bwe.details.get("nModified", 0)
        written_ids = [
            doc["_id"]
            for idx, doc in enumerate(documents)
            if idx not in error_indices
        ]
        log.warning("Bulk write con %d errores en este batch.", len(errors))
        for err in errors:
            log.debug("  Error id=%s: %s", err.get("keyValue"), err.get("errmsg"))
        return written, len(errors), written_ids


# ─── Estado de migración (para rollback manual) ───────────────────────────────

def save_migration_state(migrated_ids: list[str], pre_existing_ids: set[str]) -> None:
    state = {
        "migrated_at": datetime.now(timezone.utc).isoformat(),
        "migrated_ids": migrated_ids,
        "pre_existing_ids": list(pre_existing_ids),
    }
    _STATE_FILE.write_text(json.dumps(state, indent=2), encoding="utf-8")
    log.info("Estado de migración guardado en: %s", _STATE_FILE.name)
    log.info("  → Para deshacer ejecutá: python migrate_mysql_to_mongo.py rollback")


def load_migration_state() -> dict:
    if not _STATE_FILE.exists():
        log.error(
            "No se encontró el archivo de estado '%s'. "
            "Ejecutá primero una migración.",
            _STATE_FILE.name,
        )
        sys.exit(1)
    state = json.loads(_STATE_FILE.read_text(encoding="utf-8"))
    log.info("Estado de migración cargado  (fecha: %s)", state.get("migrated_at", "desconocida"))
    return state


# ─── Rollback ─────────────────────────────────────────────────────────────────

def do_rollback(collection, migrated_ids: list[str], pre_existing_ids: set[str]) -> None:
    ids_to_remove = [mid for mid in migrated_ids if mid not in pre_existing_ids]

    if not ids_to_remove:
        log.info("Rollback: no hay documentos nuevos que revertir.")
        return

    log.warning(
        "Iniciando rollback: eliminando %d documentos insertados por el script...",
        len(ids_to_remove),
    )

    total_deleted = 0
    chunk_size = 1000
    try:
        for i in range(0, len(ids_to_remove), chunk_size):
            chunk = ids_to_remove[i: i + chunk_size]
            result = collection.delete_many({"_id": {"$in": chunk}})
            total_deleted += result.deleted_count
            log.debug("  Batch eliminado: %d docs", result.deleted_count)

        log.warning(
            "Rollback completado. Eliminados: %d / %d.",
            total_deleted,
            len(ids_to_remove),
        )
    except PyMongoError as e:
        log.error(
            "ERROR durante el rollback: %s. "
            "IDs pendientes de eliminar: %s",
            e,
            ids_to_remove[i:],
        )


# ─── Subcomandos ──────────────────────────────────────────────────────────────

def cmd_check(args: argparse.Namespace) -> None:
    log.info("=" * 50)
    log.info("VERIFICANDO CONECTIVIDAD")
    log.info("=" * 50)

    # MySQL
    conn = connect_mysql(args)
    try:
        users = fetch_users(conn)
        log.info("Usuarios disponibles para migrar: %d", len(users))
    finally:
        conn.close()

    # MongoDB
    client = connect_mongo(args)
    collection = client[args.mongo_db][args.mongo_collection]
    count = collection.count_documents({})
    log.info(
        "Documentos actuales en MongoDB  %s.%s: %d",
        args.mongo_db,
        args.mongo_collection,
        count,
    )
    client.close()

    log.info("=" * 50)
    log.info("Conectividad OK — listo para migrar.")


def cmd_migrate(args: argparse.Namespace) -> None:
    log.info("=" * 50)
    log.info("INICIANDO MIGRACIÓN MySQL → MongoDB")
    log.info("=" * 50)

    # 1. Leer de MySQL
    mysql_conn = connect_mysql(args)
    try:
        users = fetch_users(mysql_conn)
    finally:
        mysql_conn.close()

    if not users:
        log.warning("No hay usuarios en MySQL. Nada que migrar.")
        return

    # 2. Dry-run
    if args.dry_run:
        log.info("[DRY-RUN] Se migrarían %d usuarios. No se escribió nada en MongoDB.", len(users))
        for u in users[:5]:
            log.info("  Ejemplo → id=%s  login=%s  role=%s", u["id"], u["login"], u["role"])
        if len(users) > 5:
            log.info("  ... y %d más.", len(users) - 5)
        return

    # 3. Conectar a MongoDB
    mongo_client = connect_mongo(args)
    db = mongo_client[args.mongo_db]
    collection = db[args.mongo_collection]

    # 4. Snapshot de IDs pre-existentes
    pre_existing_ids = snapshot_existing_ids(collection)

    # 5. Índice único en login
    collection.create_index("login", unique=True, background=True)

    # 6. Migrar en batches
    documents = [mysql_row_to_document(row) for row in users]
    total = len(documents)
    total_written = 0
    total_errors = 0
    all_migrated_ids: list[str] = []

    log.info("Rollback automático ACTIVADO ante cualquier fallo.")
    log.info("Migrando %d usuarios en batches de %d...", total, args.batch_size)

    try:
        for i in range(0, total, args.batch_size):
            batch = documents[i: i + args.batch_size]
            written, errors, written_ids = upsert_batch(collection, batch)
            total_written += written
            total_errors += errors
            all_migrated_ids.extend(written_ids)

            log.info(
                "  Batch %d/%d → escritos: %d  errores: %d",
                min(i + args.batch_size, total), total,
                written, errors,
            )

    except Exception as exc:
        log.error("Error inesperado: %s", exc)
        log.warning("Ejecutando rollback automático...")
        do_rollback(collection, all_migrated_ids, pre_existing_ids)
        mongo_client.close()
        sys.exit(1)

    # 7. Guardar estado para posible rollback manual
    save_migration_state(all_migrated_ids, pre_existing_ids)

    # 8. Rollback automático si hubo errores parciales
    if total_errors > 0:
        log.error(
            "%d documentos fallaron. Ejecutando rollback automático de los %d escritos...",
            total_errors,
            len(all_migrated_ids),
        )
        do_rollback(collection, all_migrated_ids, pre_existing_ids)
        mongo_client.close()
        log.error("Migración revertida por errores parciales.")
        sys.exit(2)

    mongo_client.close()

    log.info("=" * 50)
    log.info("Migración completada exitosamente.")
    log.info("  Total en MySQL        : %d", total)
    log.info("  Escritos/actualizados : %d", total_written)
    log.info("  Errores               : %d", total_errors)
    log.info("  Base destino          : %s.%s", args.mongo_db, args.mongo_collection)


def cmd_rollback(args: argparse.Namespace) -> None:
    log.info("=" * 50)
    log.info("ROLLBACK MANUAL")
    log.info("=" * 50)

    state = load_migration_state()
    migrated_ids    = state["migrated_ids"]
    pre_existing_ids = set(state["pre_existing_ids"])

    log.info(
        "Documentos insertados en la última sesión : %d",
        len([m for m in migrated_ids if m not in pre_existing_ids]),
    )
    log.info("Documentos pre-existentes (no se tocarán): %d", len(pre_existing_ids))

    mongo_client = connect_mongo(args)
    collection = mongo_client[args.mongo_db][args.mongo_collection]

    do_rollback(collection, migrated_ids, pre_existing_ids)

    mongo_client.close()

    # Eliminar el archivo de estado tras rollback exitoso
    _STATE_FILE.unlink(missing_ok=True)
    log.info("Archivo de estado eliminado.")
    log.info("=" * 50)
    log.info("Rollback completado. La colección está como antes de la migración.")


# ─── Entry point ─────────────────────────────────────────────────────────────

if __name__ == "__main__":
    args = parse_args()
    if getattr(args, "verbose", False):
        logging.getLogger().setLevel(logging.DEBUG)

    if args.command == "check":
        cmd_check(args)
    elif args.command == "migrate":
        cmd_migrate(args)
    elif args.command == "rollback":
        cmd_rollback(args)
