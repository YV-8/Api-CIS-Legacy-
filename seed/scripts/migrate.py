#!/usr/bin/env python3
"""
CIS Platform — MySQL → MongoDB migration script (US-23)

Transfiere users, topics, ideas, votes y comments desde MySQL (sd3) hacia
MongoDB (sd3) preservando los UUID originales como campo `Id` y dejando
que MongoDB genere `_id` (ObjectId). Mapea campos snake_case → PascalCase
para que coincidan con la serialización BSON por defecto del driver C# de
api-cis (TopicDocument, IdeaDocument, VoteDocument, CommentDocument).

Uso típico:
    python migrate.py --reset --validate

Modos:
    --reset        Limpia las colecciones Mongo antes de migrar (destructivo).
    --clean-only   Solo limpia las colecciones, no migra nada.
    --validate     Compara conteos MySQL vs Mongo al terminar.
"""

import argparse
import os
import sys
import time
from datetime import datetime

import mysql.connector
import pymongo
from dotenv import load_dotenv
from pymongo.errors import BulkWriteError


# ──────────────────────────────────────────────────────────────────────────────
# Mapeos MySQL → MongoDB
# ──────────────────────────────────────────────────────────────────────────────
#
# Cada entrada describe cómo transformar una fila del cursor DictCursor de
# MySQL en un documento Mongo. `field_map` es {col_mysql: campo_mongo};
# si una columna no aparece, se descarta.
#
# Importante: los nombres en PascalCase deben coincidir EXACTAMENTE con los
# *Document.cs en CIS.DataAcces/MongoDB/Documents/.
#
# Para `topics` hay dos renombres no triviales:
#   - `name`      → `Title`
#   - `user_id`   → `CreatedBy`
# y se descartan tres columnas (vote_type, allow_comments, anonymous_vote)
# porque TopicDocument no las contiene.

COLLECTIONS_ORDER = ["users", "topics", "ideas", "votes", "comments"]

FIELD_MAPS: dict[str, dict[str, str]] = {
    "users": {
        "id": "Id",
        "name": "Name",
        "login": "Login",
        "password": "Password",
        "role": "Role",
    },
    "topics": {
        "id": "Id",
        "name": "Title",
        "description": "Description",
        "type": "Type",
        "status": "Status",
        "user_id": "CreatedBy",
        "created_at": "CreatedAt",
        "updated_at": "UpdatedAt",
        "deleted_at": "DeletedAt",
    },
    "ideas": {
        "id": "Id",
        "title": "Title",
        "description": "Description",
        "topic_id": "TopicId",
        "author_id": "AuthorId",
        "vote_count": "VoteCount",
        "created_at": "CreatedAt",
        "updated_at": "UpdatedAt",
        "deleted_at": "DeletedAt",
    },
    "votes": {
        "id": "Id",
        "idea_id": "IdeaId",
        "user_id": "UserId",
        "created_at": "CreatedAt",
    },
    "comments": {
        "id": "Id",
        "idea_id": "IdeaId",
        "content": "Content",
        "user_id": "UserId",
        "created_at": "CreatedAt",
        "updated_at": "UpdatedAt",
    },
}


# ──────────────────────────────────────────────────────────────────────────────
# Helpers
# ──────────────────────────────────────────────────────────────────────────────

def transform_row(row: dict, field_map: dict[str, str]) -> dict:
    """Renombra las claves de `row` segun `field_map` y descarta las no listadas."""
    return {mongo_key: row[mysql_key] for mysql_key, mongo_key in field_map.items() if mysql_key in row}


class TeeLogger:
    """Escribe simultaneamente a stdout y a un archivo de log."""

    def __init__(self, log_path: str):
        self.file = open(log_path, "w", encoding="utf-8")

    def write(self, msg: str = "") -> None:
        print(msg)
        self.file.write(msg + "\n")
        self.file.flush()

    def close(self) -> None:
        self.file.close()


# ──────────────────────────────────────────────────────────────────────────────
# Migracion por coleccion
# ──────────────────────────────────────────────────────────────────────────────

def migrate_collection(
    log: TeeLogger,
    cursor,
    mdb,
    table: str,
    batch_size: int,
) -> tuple[int, int]:
    """Migra una tabla MySQL a su coleccion Mongo homonima.

    Devuelve (filas_leidas, docs_insertados).
    """
    field_map = FIELD_MAPS[table]
    collection = mdb[table]

    cursor.execute(f"SELECT COUNT(*) AS total FROM `{table}`")
    total = cursor.fetchone()["total"]
    log.write(f"  [{table}] {total:,} filas a migrar")
    if total == 0:
        return 0, 0

    cursor.execute(f"SELECT * FROM `{table}`")

    inserted = 0
    batch: list[dict] = []
    while True:
        rows = cursor.fetchmany(batch_size)
        if not rows:
            break
        batch = [transform_row(r, field_map) for r in rows]
        try:
            result = collection.insert_many(batch, ordered=False)
            inserted += len(result.inserted_ids)
        except BulkWriteError as bwe:
            inserted += bwe.details.get("nInserted", 0)
            log.write(f"  [{table}] AVISO: {len(bwe.details.get('writeErrors', []))} filas con error")
        pct = inserted / total * 100
        print(f"  [{table}] {inserted:,}/{total:,} ({pct:.0f}%)", end="\r", flush=True)

    log.write(f"  [{table}] {inserted:,}/{total:,} (100%) OK                       ")
    return total, inserted


# ──────────────────────────────────────────────────────────────────────────────
# Indices
# ──────────────────────────────────────────────────────────────────────────────

def ensure_indexes(log: TeeLogger, mdb) -> None:
    """Crea indices alineados con los del esquema MySQL.

    - Indice unico (IdeaId, UserId) en votes — replica el unique de MySQL.
    - Indice en `Id` en cada coleccion (lookup por UUID original).
    """
    log.write("\n── Indices ───────────────────────────────────────────")

    for col in COLLECTIONS_ORDER:
        mdb[col].create_index([("Id", pymongo.ASCENDING)], name="idx_id")
        log.write(f"  [{col}] idx_id OK")

    # Unique parcial: ignora votos anonimos (UserId null) — replica el
    # comportamiento de MySQL donde NULLs no disparan la unique constraint.
    mdb["votes"].create_index(
        [("IdeaId", pymongo.ASCENDING), ("UserId", pymongo.ASCENDING)],
        name="uniq_idea_user",
        unique=True,
        partialFilterExpression={"UserId": {"$type": "string"}},
    )
    log.write("  [votes] uniq_idea_user OK (UNIQUE IdeaId+UserId, partial)")

    mdb["ideas"].create_index([("TopicId", pymongo.ASCENDING)], name="idx_topic")
    mdb["comments"].create_index([("IdeaId", pymongo.ASCENDING)], name="idx_idea")
    log.write("  [ideas] idx_topic OK")
    log.write("  [comments] idx_idea OK")


# ──────────────────────────────────────────────────────────────────────────────
# Limpieza
# ──────────────────────────────────────────────────────────────────────────────

def confirm_clean(mode: str) -> bool:
    print(f"\nATENCION: --{mode} eliminara TODAS las colecciones en MongoDB:")
    print(f"  {', '.join(COLLECTIONS_ORDER)}")
    answer = input("Continuar? [s/N] ")
    return answer.strip().lower() == "s"


def clean_collections(log: TeeLogger, mdb) -> None:
    log.write("\nLimpiando colecciones MongoDB...")
    for col in reversed(COLLECTIONS_ORDER):
        before = mdb[col].count_documents({})
        mdb[col].drop()
        log.write(f"  {col}: {before:,} documentos eliminados OK")
    log.write("Limpieza completada.")


# ──────────────────────────────────────────────────────────────────────────────
# Validacion
# ──────────────────────────────────────────────────────────────────────────────

def validate_counts(log: TeeLogger, cursor, mdb) -> bool:
    """Compara COUNT(*) MySQL vs countDocuments Mongo. True si todo coincide."""
    log.write("\n── Validacion de conteos ─────────────────────────────")
    log.write(f"  {'Tabla':<12} {'MySQL':>10} {'Mongo':>10}  Estado")
    log.write(f"  {'-' * 12} {'-' * 10} {'-' * 10}  ------")

    all_ok = True
    for col in COLLECTIONS_ORDER:
        cursor.execute(f"SELECT COUNT(*) AS total FROM `{col}`")
        mysql_count = cursor.fetchone()["total"]
        mongo_count = mdb[col].count_documents({})
        ok = mysql_count == mongo_count
        all_ok &= ok
        marker = "OK" if ok else f"FALLO (diff={mongo_count - mysql_count:+,})"
        log.write(f"  {col:<12} {mysql_count:>10,} {mongo_count:>10,}  {marker}")

    log.write("")
    log.write("Resultado de la validacion: " + ("EXITOSO" if all_ok else "FALLO"))
    return all_ok


# ──────────────────────────────────────────────────────────────────────────────
# Configuracion / entry point
# ──────────────────────────────────────────────────────────────────────────────

def parse_args() -> argparse.Namespace:
    load_dotenv()

    parser = argparse.ArgumentParser(
        description="CIS Platform — MySQL → MongoDB migration script (US-23)"
    )
    parser.add_argument("--mysql-host",     default=os.getenv("DB_HOST", "localhost"))
    parser.add_argument("--mysql-port",     type=int, default=int(os.getenv("DB_PORT", "3307")))
    parser.add_argument("--mysql-db",       default=os.getenv("DB_NAME", "sd3"))
    parser.add_argument("--mysql-user",     default=os.getenv("DB_USER", "root"))
    parser.add_argument("--mysql-password", default=os.getenv("DB_PASSWORD", ""))
    parser.add_argument("--mongo-uri",      default=os.getenv("MONGO_URI", "mongodb://localhost:27017"))
    parser.add_argument("--mongo-db",       default=os.getenv("MONGO_DB", "sd3"))
    parser.add_argument("--batch",          type=int, default=int(os.getenv("BATCH_SIZE", "1000")))
    parser.add_argument(
        "--reset", action="store_true",
        help="Limpia las colecciones Mongo antes de migrar (destructivo).",
    )
    parser.add_argument(
        "--clean-only", action="store_true",
        help="Solo limpia las colecciones Mongo, no migra nada.",
    )
    parser.add_argument(
        "--validate", action="store_true",
        help="Compara conteos MySQL vs Mongo al terminar (exit 1 si difieren).",
    )
    parser.add_argument(
        "--yes", action="store_true",
        help="Omite la confirmacion interactiva en --reset y --clean-only.",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    log_path = f"migration_log_{datetime.now().strftime('%Y%m%d_%H%M%S')}.txt"
    log = TeeLogger(log_path)

    log.write("=" * 60)
    log.write("CIS Platform — Migration Script (US-23)")
    log.write("=" * 60)
    log.write(f"  MySQL:    {args.mysql_db} @ {args.mysql_host}:{args.mysql_port}")
    log.write(f"  MongoDB:  {args.mongo_db} @ {args.mongo_uri}")
    log.write(f"  Batch:    {args.batch:,}")
    log.write(f"  Modo:     {'clean-only' if args.clean_only else ('reset+migrate' if args.reset else 'migrate')}")
    log.write(f"  Log file: {log_path}")
    log.write("")

    # ── Conexiones ────────────────────────────────────────────────────────────
    try:
        sql = mysql.connector.connect(
            host=args.mysql_host,
            port=args.mysql_port,
            database=args.mysql_db,
            user=args.mysql_user,
            password=args.mysql_password,
            charset="utf8mb4",
            collation="utf8mb4_unicode_ci",
            autocommit=True,
        )
    except mysql.connector.Error as err:
        log.write(f"ERROR de conexion a MySQL: {err}")
        log.close()
        sys.exit(1)

    cursor = sql.cursor(dictionary=True)

    try:
        mongo_client = pymongo.MongoClient(args.mongo_uri, serverSelectionTimeoutMS=5000)
        mongo_client.admin.command("ping")
    except Exception as err:
        log.write(f"ERROR de conexion a MongoDB: {err}")
        sql.close()
        log.close()
        sys.exit(1)

    mdb = mongo_client[args.mongo_db]

    # ── Modo clean-only ───────────────────────────────────────────────────────
    if args.clean_only:
        if not args.yes and not confirm_clean("clean-only"):
            log.write("Cancelado.")
            sql.close()
            mongo_client.close()
            log.close()
            sys.exit(0)
        clean_collections(log, mdb)
        sql.close()
        mongo_client.close()
        log.write("\nColecciones limpias. No se migraron datos.")
        log.close()
        sys.exit(0)

    # ── Modo reset (limpia y luego migra) ─────────────────────────────────────
    if args.reset:
        if not args.yes and not confirm_clean("reset"):
            log.write("Cancelado.")
            sql.close()
            mongo_client.close()
            log.close()
            sys.exit(0)
        clean_collections(log, mdb)

    # ── Migracion ─────────────────────────────────────────────────────────────
    t_start = time.perf_counter()
    log.write("\n── Migracion ────────────────────────────────────────")

    totals: dict[str, tuple[int, int]] = {}
    for table in COLLECTIONS_ORDER:
        totals[table] = migrate_collection(log, cursor, mdb, table, args.batch)

    ensure_indexes(log, mdb)

    elapsed = time.perf_counter() - t_start

    log.write("")
    log.write("=" * 60)
    log.write(f"Migracion completada en {elapsed:.1f}s ({elapsed / 60:.1f} min)")
    log.write("=" * 60)
    log.write("Resumen:")
    for table, (read, written) in totals.items():
        log.write(f"  {table:<12} MySQL: {read:>10,}  →  Mongo: {written:>10,}")

    # ── Validacion opcional ───────────────────────────────────────────────────
    exit_code = 0
    if args.validate:
        ok = validate_counts(log, cursor, mdb)
        exit_code = 0 if ok else 1

    cursor.close()
    sql.close()
    mongo_client.close()
    log.close()
    sys.exit(exit_code)


if __name__ == "__main__":
    main()
