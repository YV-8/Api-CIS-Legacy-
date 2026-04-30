#!/usr/bin/env python3

import argparse
import os
import random
import sys
import time
import uuid
from datetime import datetime, timedelta, timezone

import bcrypt
import mysql.connector
from dotenv import load_dotenv

# ──────────────────────────────────────────────────────────────────────────────
# Constantes de dominio
# ──────────────────────────────────────────────────────────────────────────────

TOPIC_TYPES = ["election", "market_research", "survey", "other"]
TOPIC_STATUSES = ["active", "active", "active", "active", "draft", "inactive", "archive"]
VOTE_TYPES = ["single", "single", "single", "multiple"]
ROLES = ["USER"] * 85 + ["OWNER"] * 12 + ["ADMIN"] * 3  # distribución porcentual

FIRST_NAMES = [
    "Ana", "Carlos", "María", "Luis", "Sofía", "Juan", "Valeria", "Diego",
    "Camila", "Andrés", "Isabella", "Sergio", "Natalia", "Miguel", "Laura",
    "Felipe", "Daniela", "Ricardo", "Paola", "Jorge", "Alejandra", "Mateo",
    "Claudia", "Sebastián", "Gabriela", "Nicolás", "Fernanda", "Esteban",
    "Mariana", "Pablo", "Lucía", "Hernán", "Patricia", "Roberto", "Verónica",
]

LAST_NAMES = [
    "García", "Rodríguez", "Martínez", "López", "González", "Pérez",
    "Sánchez", "Ramírez", "Torres", "Flores", "Rivera", "Gómez", "Díaz",
    "Reyes", "Morales", "Jiménez", "Vargas", "Castillo", "Ramos", "Herrera",
    "Medina", "Aguilar", "Guerrero", "Mendoza", "Ortiz", "Silva", "Rojas",
    "Delgado", "Castro", "Núñez", "Cabrera", "Vega", "Ríos", "Fuentes",
]

TOPIC_PREFIXES = [
    "Propuesta para", "Iniciativa sobre", "Plan de mejora en", "Evaluación de",
    "Análisis de", "Estrategia para", "Proyecto de", "Revisión de",
    "Optimización de", "Implementación de",
]

TOPIC_SUBJECTS = [
    "gestión de residuos", "movilidad urbana", "salud comunitaria",
    "educación digital", "acceso al agua potable", "energías renovables",
    "seguridad ciudadana", "desarrollo económico local", "inclusión social",
    "infraestructura vial", "espacios verdes", "emprendimiento juvenil",
    "teletrabajo", "comercio electrónico", "turismo sostenible",
    "cultura y artes", "deporte comunitario", "alimentación saludable",
    "vivienda asequible", "conectividad rural", "economía circular",
    "transparencia gubernamental", "participación ciudadana",
]

IDEA_VERBS = [
    "Crear", "Desarrollar", "Implementar", "Diseñar", "Establecer",
    "Promover", "Fortalecer", "Mejorar", "Digitalizar", "Expandir",
    "Optimizar", "Integrar", "Fomentar", "Ampliar", "Modernizar",
]

IDEA_OBJECTS = [
    "un sistema de monitoreo", "una plataforma colaborativa", "un programa piloto",
    "talleres comunitarios", "una red de apoyo", "incentivos fiscales",
    "un fondo de financiamiento", "alianzas público-privadas",
    "infraestructura tecnológica", "capacitaciones especializadas",
    "campañas de sensibilización", "protocolos de seguimiento",
    "centros de atención", "canales de comunicación directa",
    "mecanismos de evaluación continua",
]

COMMENT_TEMPLATES = [
    "Excelente propuesta, creo que podría generar un gran impacto.",
    "Interesante idea, aunque habría que analizar el presupuesto requerido.",
    "Totalmente de acuerdo con esta iniciativa.",
    "¿Cómo se garantizaría la sostenibilidad a largo plazo?",
    "Me parece viable si contamos con el apoyo institucional necesario.",
    "Sería importante incluir a los beneficiarios en el diseño.",
    "Esta propuesta complementa bien otras iniciativas en marcha.",
    "Habría que definir indicadores claros de éxito.",
    "Apoyo la idea pero sugiero ampliar el alcance geográfico.",
    "¿Se ha considerado algún caso de éxito similar en otra región?",
    "Muy pertinente dado el contexto actual.",
    "Propongo añadir una fase de prueba antes del lanzamiento completo.",
    "La participación ciudadana es clave para que esto funcione.",
    "Es necesario un equipo técnico especializado para ejecutar esto.",
    "Podría combinarse con el proyecto ya aprobado el año pasado.",
]

# ──────────────────────────────────────────────────────────────────────────────
# Helpers de generación
# ──────────────────────────────────────────────────────────────────────────────

def new_id() -> str:
    return str(uuid.uuid4())


def rand_date(days_back: int = 365) -> datetime:
    delta = timedelta(days=random.randint(0, days_back), seconds=random.randint(0, 86400))
    return datetime.now(timezone.utc) - delta


def rand_name() -> str:
    return f"{random.choice(FIRST_NAMES)} {random.choice(LAST_NAMES)}"


def rand_topic_title() -> str:
    return f"{random.choice(TOPIC_PREFIXES)} {random.choice(TOPIC_SUBJECTS)}"


def rand_idea_title() -> str:
    return f"{random.choice(IDEA_VERBS)} {random.choice(IDEA_OBJECTS)}"


def rand_comment() -> str:
    return random.choice(COMMENT_TEMPLATES)


# ──────────────────────────────────────────────────────────────────────────────
# Inserción en lote
# ──────────────────────────────────────────────────────────────────────────────

def bulk_insert(conn, cursor, table: str, columns: list[str], rows: list[tuple],
                batch_size: int, ignore: bool = False) -> None:
    if not rows:
        return
    ignore_kw = "IGNORE " if ignore else ""
    col_str = ", ".join(f"`{c}`" for c in columns)
    placeholder = "(" + ", ".join(["%s"] * len(columns)) + ")"

    total = len(rows)
    inserted = 0
    for i in range(0, total, batch_size):
        chunk = rows[i : i + batch_size]
        values_clause = ", ".join([placeholder] * len(chunk))
        flat_params = [val for row in chunk for val in row]
        cursor.execute(
            f"INSERT {ignore_kw}INTO `{table}` ({col_str}) VALUES {values_clause}",
            flat_params,
        )
        conn.commit()
        inserted += len(chunk)
        pct = inserted / total * 100
        print(f"  [{table}] {inserted}/{total} ({pct:.0f}%)", end="\r", flush=True)
    print(f"  [{table}] {total}/{total} (100%) OK          ")


# ──────────────────────────────────────────────────────────────────────────────
# Funciones de seed por tabla
# ──────────────────────────────────────────────────────────────────────────────

def seed_users(conn, cursor, count: int, batch_size: int,
               password_hash: str) -> list[str]:
    """Retorna lista de IDs generados."""
    cursor.execute("SELECT COUNT(*) FROM `users`")
    existing = cursor.fetchone()[0]
    if existing >= count:
        print(f"  [users] Ya existen {existing} registros — omitiendo.")
        cursor.execute("SELECT `id` FROM `users` LIMIT %s", (count,))
        return [row[0] for row in cursor.fetchall()]

    needed = count - existing
    print(f"  [users] Generando {needed} registros (existentes: {existing})…")

    rows = []
    ids = []
    for i in range(existing + 1, existing + needed + 1):
        uid = new_id()
        ids.append(uid)
        login = f"usr_{i:05d}"          # "usr_00001" … "usr_07000" (9 chars < 20)
        name = rand_name()
        role = random.choice(ROLES)
        rows.append((uid, name, login, password_hash, role))

    bulk_insert(conn, cursor, "users",
                ["id", "name", "login", "password", "role"],
                rows, batch_size, ignore=True)

    # Recuperar todos los IDs (preexistentes + nuevos) hasta `count`
    cursor.execute("SELECT `id` FROM `users` LIMIT %s", (count,))
    return [row[0] for row in cursor.fetchall()]


def seed_topics(conn, cursor, count: int, batch_size: int,
                user_ids: list[str]) -> list[str]:
    cursor.execute("SELECT COUNT(*) FROM `topics` WHERE `deleted_at` IS NULL")
    existing = cursor.fetchone()[0]
    if existing >= count:
        print(f"  [topics] Ya existen {existing} registros — omitiendo.")
        cursor.execute("SELECT `id` FROM `topics` WHERE `deleted_at` IS NULL LIMIT %s", (count,))
        return [row[0] for row in cursor.fetchall()]

    needed = count - existing
    print(f"  [topics] Generando {needed} registros…")

    rows = []
    for _ in range(needed):
        tid = new_id()
        created = rand_date(365)
        rows.append((
            tid,
            rand_topic_title(),
            f"Descripción detallada sobre {random.choice(TOPIC_SUBJECTS)}.",
            random.choice(TOPIC_TYPES),
            random.choice(TOPIC_STATUSES),
            random.choice(user_ids),
            random.choice(VOTE_TYPES),
            random.choice([True, False]),
            random.choice([True, False]),
            created,
            created,
            None,   # deleted_at
        ))

    bulk_insert(conn, cursor, "topics",
                ["id", "name", "description", "type", "status", "user_id",
                 "vote_type", "allow_comments", "anonymous_vote",
                 "created_at", "updated_at", "deleted_at"],
                rows, batch_size)

    cursor.execute("SELECT `id` FROM `topics` WHERE `deleted_at` IS NULL LIMIT %s", (count,))
    return [row[0] for row in cursor.fetchall()]


def seed_ideas(conn, cursor, count: int, batch_size: int,
               topic_ids: list[str], user_ids: list[str]) -> list[str]:
    cursor.execute("SELECT COUNT(*) FROM `ideas` WHERE `deleted_at` IS NULL")
    existing = cursor.fetchone()[0]
    if existing >= count:
        print(f"  [ideas] Ya existen {existing} registros — omitiendo.")
        cursor.execute("SELECT `id` FROM `ideas` WHERE `deleted_at` IS NULL LIMIT %s", (count,))
        return [row[0] for row in cursor.fetchall()]

    needed = count - existing
    print(f"  [ideas] Generando {needed} registros…")

    rows = []
    for _ in range(needed):
        iid = new_id()
        created = rand_date(300)
        rows.append((
            iid,
            rand_idea_title(),
            (f"Esta idea propone {random.choice(IDEA_VERBS).lower()} "
             f"{random.choice(IDEA_OBJECTS)} para abordar el problema "
             f"de {random.choice(TOPIC_SUBJECTS)} en la comunidad."),
            random.choice(topic_ids),
            random.choice(user_ids),
            0,      # vote_count — se actualiza tras insertar votes
            created,
            created,
            None,   # deleted_at
        ))

    bulk_insert(conn, cursor, "ideas",
                ["id", "title", "description", "topic_id", "author_id",
                 "vote_count", "created_at", "updated_at", "deleted_at"],
                rows, batch_size)

    cursor.execute("SELECT `id` FROM `ideas` WHERE `deleted_at` IS NULL LIMIT %s", (count,))
    return [row[0] for row in cursor.fetchall()]


def seed_votes(conn, cursor, count: int, batch_size: int,
               idea_ids: list[str], user_ids: list[str]) -> None:
    cursor.execute("SELECT COUNT(*) FROM `votes`")
    existing = cursor.fetchone()[0]
    if existing >= count:
        print(f"  [votes] Ya existen {existing} registros — omitiendo.")
        return

    needed = count - existing
    print(f"  [votes] Generando {needed} registros (respetando índice único)…")

    # Garantizar unicidad (idea_id, user_id): repartir un usuario distinto por idea.
    # Si count <= len(idea_ids), asignamos 1 voto por idea con usuario aleatorio sin colisión.
    shuffled_users = user_ids.copy()
    random.shuffle(shuffled_users)

    rows = []
    seen: set[tuple[str, str]] = set()

    # Para votos adicionales más allá de 1 por idea, rotar usuarios por idea
    idea_vote_map: dict[str, set[str]] = {}
    user_pool = user_ids.copy()

    ideas_sample = idea_ids * (needed // len(idea_ids) + 1)
    random.shuffle(ideas_sample)

    for idea_id in ideas_sample:
        if len(rows) >= needed:
            break
        if idea_id not in idea_vote_map:
            idea_vote_map[idea_id] = set()

        remaining_users = [u for u in user_pool if u not in idea_vote_map[idea_id]]
        if not remaining_users:
            continue

        user_id = random.choice(remaining_users)
        idea_vote_map[idea_id].add(user_id)
        pair = (idea_id, user_id)
        if pair in seen:
            continue
        seen.add(pair)

        rows.append((
            new_id(),
            idea_id,
            user_id,
            rand_date(200),
        ))

    bulk_insert(conn, cursor, "votes",
                ["id", "idea_id", "user_id", "created_at"],
                rows[:needed], batch_size, ignore=True)

    # Actualizar vote_count desnormalizado en ideas
    print("  [ideas.vote_count] Actualizando contadores…", end=" ", flush=True)
    cursor.execute("""
        UPDATE `ideas` i
        SET `vote_count` = (
            SELECT COUNT(*) FROM `votes` v WHERE v.idea_id = i.id
        )
    """)
    conn.commit()
    print("OK")


def seed_comments(conn, cursor, count: int, batch_size: int,
                  idea_ids: list[str], user_ids: list[str]) -> None:
    cursor.execute("SELECT COUNT(*) FROM `comments`")
    existing = cursor.fetchone()[0]
    if existing >= count:
        print(f"  [comments] Ya existen {existing} registros — omitiendo.")
        return

    needed = count - existing
    print(f"  [comments] Generando {needed} registros…")

    rows = []
    for _ in range(needed):
        created = rand_date(250)
        rows.append((
            new_id(),
            rand_comment(),
            random.choice(idea_ids),
            random.choice(user_ids),
            created,
            created,
        ))

    bulk_insert(conn, cursor, "comments",
                ["id", "content", "idea_id", "user_id", "created_at", "updated_at"],
                rows, batch_size)


# ──────────────────────────────────────────────────────────────────────────────
# Configuración y punto de entrada
# ──────────────────────────────────────────────────────────────────────────────

def parse_args() -> argparse.Namespace:
    load_dotenv()

    parser = argparse.ArgumentParser(
        description="CIS Platform — seed script (US-27)"
    )
    parser.add_argument("--host",     default=os.getenv("DB_HOST", "localhost"))
    parser.add_argument("--port",     type=int, default=int(os.getenv("DB_PORT", "3307")))
    parser.add_argument("--db",       default=os.getenv("DB_NAME", "sd3"))
    parser.add_argument("--user",     default=os.getenv("DB_USER", "root"))
    parser.add_argument("--password", default=os.getenv("DB_PASSWORD", ""))
    parser.add_argument("--users",    type=int, default=int(os.getenv("USERS_COUNT", "7000")))
    parser.add_argument("--topics",   type=int, default=int(os.getenv("TOPICS_COUNT", "7000")))
    parser.add_argument("--ideas",    type=int, default=int(os.getenv("IDEAS_COUNT", "7000")))
    parser.add_argument("--votes",    type=int, default=int(os.getenv("VOTES_COUNT", "7000")))
    parser.add_argument("--comments", type=int, default=int(os.getenv("COMMENTS_COUNT", "14000")))
    parser.add_argument("--batch",    type=int, default=int(os.getenv("BATCH_SIZE", "500")))
    parser.add_argument(
        "--reset", action="store_true",
        help="Limpia todas las tablas y vuelve a sembrar (destructivo)"
    )
    parser.add_argument(
        "--clean-only", action="store_true",
        help="Solo limpia las tablas sin resembrar datos"
    )
    return parser.parse_args()


def confirm_clean(mode: str) -> bool:
    print(f"\nATENCION: --{mode} eliminara TODOS los datos existentes en las tablas:")
    print("  comments, votes, ideas, topics, users")
    answer = input("Continuar? [s/N] ")
    return answer.strip().lower() == "s"


def clean_tables(conn, cursor) -> None:
    """Borra todos los registros respetando FK constraints usando DELETE FROM."""
    print("\nLimpiando tablas...")
    cursor.execute("SET FOREIGN_KEY_CHECKS = 0")
    conn.commit()

    for table in ["comments", "votes", "ideas", "topics", "users"]:
        try:
            cursor.execute(f"SELECT COUNT(*) FROM `{table}`")
            before = cursor.fetchone()[0]
            cursor.execute(f"DELETE FROM `{table}`")
            conn.commit()
            cursor.execute(f"SELECT COUNT(*) FROM `{table}`")
            after = cursor.fetchone()[0]
            if after == 0:
                print(f"  {table}: {before} registros eliminados OK")
            else:
                print(f"  {table}: ADVERTENCIA — quedaron {after} registros", file=sys.stderr)
        except mysql.connector.Error as err:
            print(f"  {table}: ERROR al limpiar — {err}", file=sys.stderr)
            conn.rollback()
            cursor.execute("SET FOREIGN_KEY_CHECKS = 1")
            conn.commit()
            conn.close()
            sys.exit(1)

    cursor.execute("SET FOREIGN_KEY_CHECKS = 1")
    conn.commit()
    print("Limpieza completada.\n")


def main() -> None:
    args = parse_args()

    print("=" * 60)
    print("CIS Platform — Seed Script (US-27)")
    print("=" * 60)
    print(f"  BD:        {args.db} @ {args.host}:{args.port}")
    print(f"  Usuarios:  {args.users:,}")
    print(f"  Topics:    {args.topics:,}")
    print(f"  Ideas:     {args.ideas:,}")
    print(f"  Votos:     {args.votes:,}")
    print(f"  Comentarios: {args.comments:,}")
    print(f"  Lote:      {args.batch:,}")
    print()

    # Hash de contraseña (computado una sola vez para todos los usuarios seed)
    print("Generando hash de contraseña…", end=" ", flush=True)
    password_hash = bcrypt.hashpw(b"12345", bcrypt.gensalt(rounds=10)).decode()
    print("OK")

    try:
        conn = mysql.connector.connect(
            host=args.host,
            port=args.port,
            database=args.db,
            user=args.user,
            password=args.password,
            charset="utf8mb4",
            collation="utf8mb4_unicode_ci",
            autocommit=False,
        )
    except mysql.connector.Error as err:
        print(f"\n Error de conexión: {err}", file=sys.stderr)
        sys.exit(1)

    cursor = conn.cursor()

    # ── Modo clean-only: solo limpia y sale ───────────────────────────────────
    if args.clean_only:
        if not confirm_clean("clean-only"):
            print("Cancelado.")
            conn.close()
            sys.exit(0)
        clean_tables(conn, cursor)
        cursor.close()
        conn.close()
        print("Tablas limpias. No se resembraron datos.")
        sys.exit(0)

    # ── Modo reset: limpia y luego resembrar ──────────────────────────────────
    if args.reset:
        if not confirm_clean("reset"):
            print("Cancelado.")
            conn.close()
            sys.exit(0)
        clean_tables(conn, cursor)

    t_start = time.perf_counter()

    print("\n── Usuarios ──────────────────────────────────────────")
    user_ids = seed_users(conn, cursor, args.users, args.batch, password_hash)

    print("\n── Topics ────────────────────────────────────────────")
    topic_ids = seed_topics(conn, cursor, args.topics, args.batch, user_ids)

    print("\n── Ideas ─────────────────────────────────────────────")
    idea_ids = seed_ideas(conn, cursor, args.ideas, args.batch, topic_ids, user_ids)

    print("\n── Votos ─────────────────────────────────────────────")
    seed_votes(conn, cursor, args.votes, args.batch, idea_ids, user_ids)

    print("\n── Comentarios ───────────────────────────────────────")
    seed_comments(conn, cursor, args.comments, args.batch, idea_ids, user_ids)

    elapsed = time.perf_counter() - t_start

    cursor.close()
    conn.close()

    print()
    print("=" * 60)
    print(f" Seed completado en {elapsed:.1f}s ({elapsed/60:.1f} min)")
    print("=" * 60)

if __name__ == "__main__":
    main()
