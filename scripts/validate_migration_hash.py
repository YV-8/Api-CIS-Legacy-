#!/usr/bin/env python3
"""
CIS Platform — SHA-256 migration hash validator (US-25)

Uso:
    python validate_migration_hash.py [--id-strategy int-as-string|objectid]

Variables de entorno requeridas:
    DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD
    MONGO_URI, MONGO_DB

Codigos de salida:
    0 — todos los hashes coinciden
    1 — al menos una tabla tiene MISMATCH
"""

import argparse
import hashlib
import json
import os
import sys
from datetime import datetime, date

import mysql.connector
import pymongo
from bson import ObjectId
from dotenv import load_dotenv

load_dotenv()

MYSQL_CFG = {
    "host":     os.getenv("DB_HOST", "localhost"),
    "port":     int(os.getenv("DB_PORT", 3307)),
    "database": os.getenv("DB_NAME", "sd3"),
    "user":     os.getenv("DB_USER", "SOME_USER"),
    "password": os.getenv("DB_PASSWORD", "SOME_PASSWORD"),
}

MONGO_URI = os.getenv("MONGO_URI", "mongodb://localhost:27017")
MONGO_DB  = os.getenv("MONGO_DB", "sd3")

FIELD_MAPS: dict[str, dict[str, str]] = {
    "topics": {
        "id":          "Id",
        "name":        "Title",
        "description": "Description",
        "type":        "Type",
        "status":      "Status",
        "user_id":     "CreatedBy",
        "created_at":  "CreatedAt",
        "updated_at":  "UpdatedAt",
        "deleted_at":  "DeletedAt",
    },
    "ideas": {
        "id":          "Id",
        "title":       "Title",
        "description": "Description",
        "topic_id":    "TopicId",
        "author_id":   "AuthorId",
        "vote_count":  "VoteCount",
        "created_at":  "CreatedAt",
        "updated_at":  "UpdatedAt",
        "deleted_at":  "DeletedAt",
    },
    "votes": {
        "id":         "Id",
        "idea_id":    "IdeaId",
        "user_id":    "UserId",
        "created_at": "CreatedAt",
    },
    "comments": {
        "id":         "Id",
        "idea_id":    "IdeaId",
        "content":    "Content",
        "user_id":    "UserId",
        "created_at": "CreatedAt",
        "updated_at": "UpdatedAt",
    },
}

MYSQL_ORDER: dict[str, str] = {
    "topics":   "ORDER BY id ASC",
    "ideas":    "ORDER BY id ASC",
    "votes":    "ORDER BY idea_id ASC, user_id ASC",
    "comments": "ORDER BY id ASC",
}

MONGO_SORT: dict[str, list] = {
    "topics":   [("Id", pymongo.ASCENDING)],
    "ideas":    [("Id", pymongo.ASCENDING)],
    "votes":    [("IdeaId", pymongo.ASCENDING), ("UserId", pymongo.ASCENDING)],
    "comments": [("Id", pymongo.ASCENDING)],
}

BOOL_FIELDS = {"AllowComments", "AnonymousVote"}
TABLES = list(FIELD_MAPS.keys())


def normalize_value(v, field_name: str = "", id_strategy: str = "int-as-string"):
    if isinstance(v, datetime):
        return v.replace(microsecond=0).isoformat()
    if isinstance(v, date):
        return v.isoformat()
    if v is None:
        return None
    if isinstance(v, ObjectId):
        return str(v)
    if field_name in BOOL_FIELDS and isinstance(v, int):
        return bool(v)
    return v


def row_to_canonical(row: dict, field_map: dict[str, str], id_strategy: str) -> dict:
    return {
        field_map[k]: normalize_value(v, field_map[k], id_strategy)
        for k, v in row.items()
        if k in field_map
    }


def doc_to_canonical(doc: dict, field_map: dict[str, str], id_strategy: str) -> dict:
    return {
        field: normalize_value(doc[field], field, id_strategy)
        for field in field_map.values()
        if field in doc
    }


def compute_hash(rows: list[dict]) -> str:
    serialized = json.dumps(rows, sort_keys=True, ensure_ascii=False, default=str)
    return hashlib.sha256(serialized.encode("utf-8")).hexdigest()


def fetch_mysql(table: str, field_map: dict[str, str], id_strategy: str) -> list[dict]:
    conn = mysql.connector.connect(**MYSQL_CFG)
    cursor = conn.cursor(dictionary=True)
    cols = ", ".join(field_map.keys())
    cursor.execute(f"SELECT {cols} FROM {table} {MYSQL_ORDER[table]}")
    rows = [row_to_canonical(row, field_map, id_strategy) for row in cursor.fetchall()]
    cursor.close()
    conn.close()
    return rows


def fetch_mongo(collection: str, field_map: dict[str, str], id_strategy: str) -> list[dict]:
    client = pymongo.MongoClient(MONGO_URI)
    db = client[MONGO_DB]
    docs = list(db[collection].find({}, {"_id": 0}).sort(MONGO_SORT[collection]))
    client.close()
    return [doc_to_canonical(doc, field_map, id_strategy) for doc in docs]


def print_report(results: list[dict]) -> bool:
    all_ok = all(r["match"] for r in results)
    print()
    print("=" * 60)
    print("  INFORME DE VALIDACION SHA-256 — API CIS")
    print("=" * 60)
    for r in results:
        status = "OK      " if r["match"] else "MISMATCH"
        print(f"  [{status}]  {r['table']:12s}  MySQL:{r['mysql_count']:6d}  Mongo:{r['mongo_count']:6d}")
        if not r["match"]:
            print(f"             MySQL hash : {r['mysql_hash']}")
            print(f"             Mongo hash : {r['mongo_hash']}")
    print("-" * 60)
    global_status = "OK — todos los hashes coinciden" if all_ok else "MISMATCH — se encontraron diferencias"
    print(f"  Global: {global_status}")
    print("=" * 60)
    print()
    return all_ok


def parse_args():
    parser = argparse.ArgumentParser(description="Valida integridad SHA-256 MySQL → MongoDB")
    parser.add_argument(
        "--id-strategy",
        choices=["int-as-string", "objectid"],
        default="int-as-string",
    )
    return parser.parse_args()


def main():
    args = parse_args()
    results = []

    for table in TABLES:
        field_map = FIELD_MAPS[table]
        print(f"Procesando {table}...", end=" ", flush=True)

        mysql_rows = fetch_mysql(table, field_map, args.id_strategy)
        mongo_rows = fetch_mongo(table, field_map, args.id_strategy)

        mysql_hash = compute_hash(mysql_rows)
        mongo_hash = compute_hash(mongo_rows)

        match = mysql_hash == mongo_hash
        print("OK" if match else "MISMATCH")

        results.append({
            "table":       table,
            "mysql_count": len(mysql_rows),
            "mongo_count": len(mongo_rows),
            "mysql_hash":  mysql_hash,
            "mongo_hash":  mongo_hash,
            "match":       match,
        })

    all_ok = print_report(results)
    sys.exit(0 if all_ok else 1)


if __name__ == "__main__":
    main()
