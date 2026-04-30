import argparse
import hashlib
import json
import os
import sys
import time
from datetime import datetime
from pathlib import Path
 
import pymongo
from bson import ObjectId
from dotenv import load_dotenv
from pymongo.errors import BulkWriteError

COLLECTIONS_ORDER = ["comments", "votes", "ideas", "topics", "users"]
 
EXIT_SUCCESS  = 0
EXIT_FAILURE  = 1
EXIT_PARTIAL  = 2

class TeeLogger:
    """Writes simultaneously to stdout and a log file."""
 
    def __init__(self, log_path: str):
        Path(log_path).parent.mkdir(parents=True, exist_ok=True)
        self.file = open(log_path, "w", encoding="utf-8")
 
    def write(self, msg: str = "") -> None:
        print(msg)
        self.file.write(msg + "\n")
        self.file.flush()
 
    def close(self) -> None:
        self.file.close()

class BSONEncoder(json.JSONEncoder):
    """Converts BSON types that are not JSON serializable to str for the JSON backup."""
 
    def default(self, obj):
        if isinstance(obj, ObjectId):
            return {"$oid": str(obj)}
        if isinstance(obj, datetime):
            return {"$date": obj.isoformat()}
        return super().default(obj)
 
 
def bson_decoder(obj: dict) -> dict:
    """Restaura los tipos BSON desde el backup JSON."""
    for key, value in obj.items():
        if isinstance(value, dict):
            if "$oid" in value:
                obj[key] = ObjectId(value["$oid"])
            elif "$date" in value:
                obj[key] = datetime.fromisoformat(value["$date"])
    return obj

def create_backup(log: TeeLogger, mdb, backup_dir: str) -> str | None:
    """
    Exports all collections to a JSON file in backup_dir.
    Returns the path of the created file, or None if it fails.
    """
    Path(backup_dir).mkdir(parents=True, exist_ok=True)
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_path = Path(backup_dir) / f"backup_{timestamp}.json"
 
    log.write(f"\n Creating backup → {backup_path} ")
 
    backup_data: dict[str, list] = {}
    try:
        for col_name in reversed(COLLECTIONS_ORDER):   # users → topics → ideas …
            docs = list(mdb[col_name].find({}))
            backup_data[col_name] = docs
            log.write(f"  [{col_name}] {len(docs):,} documents exported")
 
        with open(backup_path, "w", encoding="utf-8") as f:
            json.dump(backup_data, f, cls=BSONEncoder, ensure_ascii=False, indent=2)
 
        size_kb = backup_path.stat().st_size / 1024
        log.write(f"\n  Backup save: {backup_path} ({size_kb:.1f} KB)")
        return str(backup_path)
 
    except Exception as exc:
        log.write(f"\n  ERROR create backup: {exc}")
        return None
 
 
def list_backups(backup_dir: str) -> list[Path]:
    """ Returns available backups sorted from newest to oldest """
    base = Path(backup_dir)
    if not base.exists():
        return []
    return sorted(base.glob("backup_*.json"), reverse=True)
 
 
def resolve_backup_path(target: str, backup_dir: str) -> Path | None:
    """
    Resolve the backup to use:
      - 'latest' → the latest backup
      - file name → search by exact or partial match in backup_dir
    """
    backups = list_backups(backup_dir)
    if not backups:
        return None
 
    if target == "latest":
        return backups[0]
 
    # Busqueda por nombre exacto o parcial
    for b in backups:
        if b.name == target or b.stem == target or target in b.name:
            return b
    return None

def rollback_to_zero(log: TeeLogger, mdb) -> int:
    """
    Delete all collections. Equivalent to 'revert to migration 0'.
    Returns EXIT_SUCCESS or EXIT_PARTIAL.
    """
    log.write("\n── Rollback completo (target=0) ")
    failed = []
 
    for col_name in COLLECTIONS_ORDER:
        try:
            count = mdb[col_name].count_documents({})
            mdb[col_name].drop()
            log.write(f"  [{col_name}] {count:,} Delete document OK")
        except Exception as exc:
            log.write(f"  [{col_name}] ERROR: {exc}")
            failed.append(col_name)
 
    if failed:
        log.write(f"\n  AVISO: Failed to drop {len(failed)} collections: {', '.join(failed)}")
        return EXIT_PARTIAL
 
    log.write("\n  All collections dropped successfully")
    return EXIT_SUCCESS

def rollback_from_backup(
    log: TeeLogger,
    mdb,
    backup_path: Path,
    batch_size: int,
) -> int:
    """
    Restaura las colecciones desde un archivo de backup JSON.
    Retorna EXIT_SUCCESS, EXIT_PARTIAL o EXIT_FAILURE.
    """
    log.write(f"\n Restaurando desde backup ")
    log.write(f"  Archivo: {backup_path}")
 
    # Cargar backup
    try:
        with open(backup_path, "r", encoding="utf-8") as f:
            backup_data: dict[str, list] = json.load(f, object_hook=bson_decoder)
    except Exception as exc:
        log.write(f"  ERROR leyendo backup: {exc}")
        return EXIT_FAILURE
 
    failed = []
    partial = []
 
    for col_name in COLLECTIONS_ORDER:
        docs = backup_data.get(col_name, [])
        log.write(f"\n  [{col_name}] {len(docs):,} documentos a restaurar")
 
        try:
            # Limpiar coleccion actual
            before = mdb[col_name].count_documents({})
            mdb[col_name].drop()
            log.write(f"  [{col_name}] Coleccion limpiada ({before:,} docs previos eliminados)")
 
            if not docs:
                log.write(f"  [{col_name}] Sin documentos en backup, se deja vacia.")
                continue
 
            # Insertar en batches
            inserted_total = 0
            total = len(docs)
            for i in range(0, total, batch_size):
                batch = docs[i : i + batch_size]
                try:
                    result = mdb[col_name].insert_many(batch, ordered=False)
                    inserted_total += len(result.inserted_ids)
                except BulkWriteError as bwe:
                    inserted_total += bwe.details.get("nInserted", 0)
                    log.write(
                        f"  [{col_name}] AVISO: {len(bwe.details.get('writeErrors', []))} "
                        "documentos con error en este batch"
                    )
                pct = inserted_total / total * 100
                print(f"  [{col_name}] {inserted_total:,}/{total:,} ({pct:.0f}%)", end="\r", flush=True)
 
            log.write(f"  [{col_name}] {inserted_total:,}/{total:,} restaurados OK             ")
 
            if inserted_total < total:
                partial.append(col_name)
 
        except Exception as exc:
            log.write(f"  [{col_name}] ERROR critico: {exc}")
            failed.append(col_name)
 
    # Resultado final
    if failed:
        return EXIT_PARTIAL
    if partial:
        log.write(f"\n  AVISO: Restauracion parcial en: {', '.join(partial)}")
        return EXIT_PARTIAL
    return EXIT_SUCCESS
 

def validate_restore(log: TeeLogger, mdb, backup_data: dict) -> bool:
    """Compara conteos del backup vs lo que quedo en Mongo."""
    log.write("\n Validacion post-restore")
    log.write(f"  {'Coleccion':<12} {'Backup':>10} {'Mongo':>10}  Estado")
    log.write(f"  {'-'*12} {'-'*10} {'-'*10}  ------")
 
    all_ok = True
    for col_name in reversed(COLLECTIONS_ORDER):
        expected = len(backup_data.get(col_name, []))
        actual   = mdb[col_name].count_documents({})
        ok = expected == actual
        all_ok &= ok
        marker = "OK" if ok else f"FALLO (diff={actual - expected:+,})"
        log.write(f"  {col_name:<12} {expected:>10,} {actual:>10,}  {marker}")
 
    log.write("")
    log.write("Resultado: " + ("EXITOSO" if all_ok else "CON DIFERENCIAS"))
    return all_ok
 
 

def confirm_action(action_desc: str) -> bool:
    print(f"\nATENCION: {action_desc}")
    answer = input("Continuar? [s/N] ").strip().lower()
    return answer == "s"
 
 

def parse_args() -> argparse.Namespace:
    load_dotenv()
 
    parser = argparse.ArgumentParser(
        description="CIS Platform — MongoDB rollback script (US-24)"
    )
 
    # Conexion MongoDB
    parser.add_argument("--mongo-uri",  default=os.getenv("MONGO_URI", "mongodb://localhost:27017"))
    parser.add_argument("--mongo-db",   default=os.getenv("MONGO_DB", "sd3"))
 
    # Directorios
    parser.add_argument("--backup-dir", default=os.getenv("BACKUP_DIR", "backups"))
    parser.add_argument("--log-dir",    default=os.getenv("LOG_DIR", "logs"))
    parser.add_argument("--batch",      type=int, default=int(os.getenv("BATCH_SIZE", "1000")))
 
    # Modos de operacion
    parser.add_argument(
        "--target",
        metavar="MIGRATION|0|latest",
        help=(
            "A donde revertir: '0' limpia todo, 'latest' usa el backup mas reciente, "
            "o el nombre de un archivo de backup especifico."
        ),
    )
    parser.add_argument(
        "--backup-only", action="store_true",
        help="Solo crea un backup del estado actual, no revierte nada.",
    )
    parser.add_argument(
        "--list-backups", action="store_true",
        help="Lista los backups disponibles en --backup-dir.",
    )
    parser.add_argument(
        "--no-backup", action="store_true",
        help="Omite la creacion del backup de seguridad previo al rollback.",
    )
    parser.add_argument(
        "--validate", action="store_true",
        help="Valida conteos despues del rollback (solo con --target backup).",
    )
    parser.add_argument(
        "--yes", action="store_true",
        help="Omite confirmaciones interactivas.",
    )
 
    return parser.parse_args()
 

def main() -> None:
    args = parse_args()
 
    # Modo: listar backups 
    if args.list_backups:
        backups = list_backups(args.backup_dir)
        if not backups:
            print(f"No hay backups en '{args.backup_dir}'.")
        else:
            print(f"\nBackups disponibles en '{args.backup_dir}':")
            for i, b in enumerate(backups, 1):
                size_kb = b.stat().st_size / 1024
                mtime   = datetime.fromtimestamp(b.stat().st_mtime).strftime("%Y-%m-%d %H:%M:%S")
                marker  = "  ← latest" if i == 1 else ""
                print(f"  {i:>2}. {b.name}  ({size_kb:.1f} KB)  {mtime}{marker}")
        sys.exit(EXIT_SUCCESS)
 
    #  Logger 
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    log_path  = str(Path(args.log_dir) / f"rollback_{timestamp}.log")
    log       = TeeLogger(log_path)
 
    log.write("=" * 60)
    log.write("CIS Platform — Rollback Script (US-24)")
    log.write("=" * 60)
    log.write(f"  MongoDB:    {args.mongo_db} @ {args.mongo_uri}")
    log.write(f"  Backup dir: {args.backup_dir}")
    log.write(f"  Log:        {log_path}")
    target_label = args.target if args.target else ("backup-only" if args.backup_only else "no definido")
    log.write(f"  Target:     {target_label}")
    log.write("")
 
    # Conexion Mongo 
    try:
        client = pymongo.MongoClient(args.mongo_uri, serverSelectionTimeoutMS=5000)
        client.admin.command("ping")
    except Exception as exc:
        log.write(f"ERROR de conexion a MongoDB: {exc}")
        log.close()
        sys.exit(EXIT_FAILURE)
 
    mdb = client[args.mongo_db]
 
    #Modo: solo backup 
    if args.backup_only:
        result = create_backup(log, mdb, args.backup_dir)
        client.close()
        log.close()
        sys.exit(EXIT_SUCCESS if result else EXIT_FAILURE)
 
    #  Validar que se paso --target 
    if not args.target:
        log.write("ERROR: Debes especificar --target (0, latest, o nombre de backup).")
        log.write("       Usa --list-backups para ver los backups disponibles.")
        log.write("       Usa --backup-only para solo crear un backup.")
        client.close()
        log.close()
        sys.exit(EXIT_FAILURE)
 
    t_start = time.perf_counter()
 
    #  Backup de seguridad previo al rollback 
    if not args.no_backup:
        backup_result = create_backup(log, mdb, args.backup_dir)
        if backup_result is None:
            log.write("\nERROR: No se pudo crear el backup de seguridad.")
            log.write("  Usa --no-backup para omitir esta proteccion (PELIGROSO).")
            client.close()
            log.close()
            sys.exit(EXIT_FAILURE)
    else:
        log.write("AVISO: --no-backup activo. Se omite backup de seguridad.")
 
    #Rollback a 0
    if args.target == "0":
        if not args.yes and not confirm_action(
            "The collections will be deleted from MongoDB. This action cannot be undone\n"
            "  (unless you have the security backup created in this same step)."
        ):
            log.write("Operation cancelled by the user.")
            client.close()
            log.close()
            sys.exit(EXIT_SUCCESS)
 
        exit_code = rollback_to_zero(log, mdb)
 
    # ── Rollback desde backup ─────────────────────────────────────────────
    else:
        backup_path = resolve_backup_path(args.target, args.backup_dir)
        if backup_path is None:
            log.write(f"ERROR: No se encontro backup para target='{args.target}'.")
            log.write(f"  Use --list-backups para ver los disponibles en '{args.backup_dir}'.")
            client.close()
            log.close()
            sys.exit(EXIT_FAILURE)
 
        if not args.yes and not confirm_action(
            f"The collections will be restored from:\n  {backup_path}\n"
            "  The current data in MongoDB will be replaced"
        ):
            log.write("Operation cancelled by the user")
            client.close()
            log.close()
            sys.exit(EXIT_SUCCESS)
 
        exit_code = rollback_from_backup(log, mdb, backup_path, args.batch)
 
        # Validacion opcional (solo aplica en restore desde backup)
        if args.validate and exit_code != EXIT_FAILURE:
            with open(backup_path, "r", encoding="utf-8") as f:
                backup_data = json.load(f, object_hook=bson_decoder)
            ok = validate_restore(log, mdb, backup_data)
            if not ok and exit_code == EXIT_SUCCESS:
                exit_code = EXIT_PARTIAL
 
    # Resumen final
    elapsed = time.perf_counter() - t_start
    result_label = {EXIT_SUCCESS: "SUCCESSFULLY", EXIT_PARTIAL: "PARTIAL", EXIT_FAILURE: "FAILED"}
    log.write("")
    log.write("=" * 60)
    log.write(f"Rollback completado en {elapsed:.1f}s")
    log.write(f"Resultado: {result_label.get(exit_code, 'UNKNOWN')}")
    log.write(f"Log guardado en: {log_path}")
    log.write("=" * 60)
 
    client.close()
    log.close()
    sys.exit(exit_code)
 
 
if __name__ == "__main__":
    main()
