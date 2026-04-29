#!/usr/bin/env python3
"""
seed_users.py
-------------
Pobla la tabla/colección `users` con usuarios de prueba.

Bases de datos soportadas: MySQL (default) | MongoDB

Uso rápido
----------
  # Insertar 50 usuarios en MySQL (valores por defecto):
  python seed_users.py

  # Insertar 200 usuarios:
  python seed_users.py --count 200

  # Insertar en MongoDB:
  python seed_users.py --db mongodb --count 100

Variables de entorno (o archivo .env en la raíz del proyecto)
-------------------------------------------------------------
  MySQL   → DB_HOST, DB_PORT, DB_NAME, DB_USERNAME, DB_PASSWORD
  MongoDB → MONGODB_URI, MONGO_DB_NAME
"""

import argparse
import os
import sys
import uuid
from pathlib import Path

import random

import bcrypt
from dotenv import load_dotenv
from faker import Faker

# ── Cargar .env desde la raíz del proyecto ────────────────────────────────────
ROOT_DIR = Path(__file__).resolve().parents[2]
load_dotenv(ROOT_DIR / ".env")

fake = Faker("es_ES")

# ── Roles disponibles (deben coincidir con el enum Role del microservicio) ─────
ROLES = ["ADMIN", "OWNER", "USER"]

# ── Probabilidad de cada rol (suma 1.0) ───────────────────────────────────────
ROLE_WEIGHTS = [0.1, 0.2, 0.7]  # 10% ADMIN, 20% OWNER, 70% USER


# ─────────────────────────────────────────────────────────────────────────────
# Generación de datos
# ─────────────────────────────────────────────────────────────────────────────

def hash_password(plain: str) -> str:
    """Devuelve el hash BCrypt de la contraseña (compatible con Spring Security)."""
    return bcrypt.hashpw(plain.encode(), bcrypt.gensalt()).decode()


def generate_user(index: int) -> dict:
    """
    Genera un usuario con datos aleatorios.
    `index` se usa para garantizar logins únicos.
    """
    name = fake.name()[:200]
    login = f"{fake.user_name()}{index}"[:20]
    plain_password = "12345"          # contraseña fija para pruebas
    role = random.choices(ROLES, weights=ROLE_WEIGHTS, k=1)[0]

    return {
        "id": str(uuid.uuid4()),
        "name": name,
        "login": login,
        "password": hash_password(plain_password),
        "role": role,
    }


def generate_users(count: int) -> list[dict]:
    """Genera una lista de `count` usuarios únicos."""
    print(f"Generando {count} usuarios...")
    return [generate_user(i) for i in range(1, count + 1)]


# ─────────────────────────────────────────────────────────────────────────────
# MySQL
# ─────────────────────────────────────────────────────────────────────────────

def seed_mysql(users: list[dict]) -> None:
    try:
        import pymysql
    except ImportError:
        sys.exit("ERROR: pymysql no está instalado. Ejecuta: pip install pymysql")

    config = {
        "host":     os.getenv("DB_HOST", "localhost"),
        "port":     int(os.getenv("DB_PORT", 3307)),
        "db":       os.getenv("DB_NAME", "sd3"),
        "user":     os.getenv("DB_USERNAME", "root"),
        "password": os.getenv("DB_PASSWORD", ""),
        "charset":  "utf8mb4",
        "cursorclass": pymysql.cursors.DictCursor,
    }

    print(f"Conectando a MySQL en {config['host']}:{config['port']}/{config['db']}...")

    conn = pymysql.connect(**config)
    try:
        with conn.cursor() as cursor:
            sql = """
                INSERT INTO users (id, name, login, password, role)
                VALUES (%s, %s, %s, %s, %s)
            """
            rows = [(u["id"], u["name"], u["login"], u["password"], u["role"])
                    for u in users]
            cursor.executemany(sql, rows)
        conn.commit()
        print(f"OK: {len(users)} usuarios insertados en MySQL.")
    except Exception as exc:
        conn.rollback()
        sys.exit(f"ERROR al insertar en MySQL: {exc}")
    finally:
        conn.close()


# ─────────────────────────────────────────────────────────────────────────────
# MongoDB
# ─────────────────────────────────────────────────────────────────────────────

def seed_mongodb(users: list[dict]) -> None:
    try:
        from pymongo import MongoClient
    except ImportError:
        sys.exit("ERROR: pymongo no está instalado. Ejecuta: pip install pymongo")

    uri     = os.getenv("MONGODB_URI", "mongodb://localhost:27017")
    db_name = os.getenv("MONGO_DB_NAME", "sd3")

    print(f"Conectando a MongoDB ({db_name})...")

    client = MongoClient(uri)
    try:
        db = client[db_name]
        result = db["users"].insert_many(users)
        print(f"OK: {len(result.inserted_ids)} usuarios insertados en MongoDB.")
    except Exception as exc:
        sys.exit(f"ERROR al insertar en MongoDB: {exc}")
    finally:
        client.close()


# ─────────────────────────────────────────────────────────────────────────────
# Utilidades
# ─────────────────────────────────────────────────────────────────────────────

def _preview(users: list[dict]) -> None:
    """Muestra una vista previa de los primeros 5 usuarios generados."""
    print("\nPrimeros 5 usuarios generados (contraseña hasheada):")
    print(f"{'#':<4} {'login':<22} {'name':<30} {'role':<8}")
    print("-" * 68)
    for i, u in enumerate(users[:5], 1):
        print(f"{i:<4} {u['login']:<22} {u['name']:<30} {u['role']:<8}")
    if len(users) > 5:
        print(f"  ... y {len(users) - 5} más")


# ─────────────────────────────────────────────────────────────────────────────
# CLI
# ─────────────────────────────────────────────────────────────────────────────

def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Pobla la tabla/colección `users` con datos de prueba.",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    parser.add_argument(
        "--count", "-n",
        type=int,
        default=50,
        help="Cantidad de usuarios a insertar.",
    )
    parser.add_argument(
        "--db",
        choices=["mysql", "mongodb"],
        default="mysql",
        help="Base de datos destino.",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    users = generate_users(args.count)

    if args.db == "mysql":
        seed_mysql(users)
    else:
        seed_mongodb(users)

    _preview(users)


if __name__ == "__main__":
    main()
