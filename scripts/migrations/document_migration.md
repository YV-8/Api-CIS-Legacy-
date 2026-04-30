  ## Summary

  Implementa el script automatizado de migración de datos MySQL → MongoDB
  para api-cis (US-23). Permite cumplir la ventana de ≤15 min del plan de
  migración fase 3 sobre la base de datos `sd3`.

  ## Cambios

  | Archivo | Tipo | Descripción |
  |---------|------|-------------|
  | `Codigo/CIS/api-cis/seed/migrate.py` | nuevo | Script principal de migración |
  | `Codigo/CIS/api-cis/seed/requirements.txt` | mod | Agrega `pymongo>=4.6.0` |
  | `Codigo/CIS/api-cis/seed/.env.example` | mod | Agrega `MONGO_URI` y `MONGO_DB` |

  No se toca código C# — la capa dual de persistencia (`Persistence:Provider`)
  ya estaba lista 


  ## Cómo probarlo

  ```bash
  cd Codigo/CIS/api-cis/seed
  pip install -r requirements.txt
  cp .env.example .env   # editar credenciales si hace falta

  # 1. seed inicial en MySQL si la BD está vacía
  python seed.py --users 1000 --topics 500 --ideas 2000 --votes 5000 --comments 3000

  # 2. levantar mongo
  docker compose -f ../docker-compose.yml up -d mongo

  # 3. migrar + validar
  python migrate.py --reset --validate

  Verificaciones esperadas:
  - mongosh sd3 --eval 'db.topics.countDocuments()' coincide con el COUNT de MySQL.
  - db.topics.findOne() muestra campos Title y CreatedBy (no name / user_id).
  - db.votes.getIndexes() muestra uniq_idea_user con unique: true y partialFilterExpression.
  - migration_log_*.txt se genera con el resumen.

  Acceptance criteria

  - Script transfiere users, topics, ideas, votes (y comments) a las
  colecciones correspondientes en MongoDB.
  - Migración completa < 15 min (script ~1 min sobre 30k registros).
  - ID mapping correcto: ObjectId para _id, UUID string para Id.

  
  
  
  ## Decisiones técnicas

  - **IDs**: `_id` autogenerado por Mongo (ObjectId → mapea a `MongoId` en
    los `*Document.cs`); UUIDs originales de MySQL preservados en el campo
    `Id`. Cumple la nota técnica del issue.
  - **Mapeo PascalCase** alineado con BSON serialization del driver C#.
    Renames críticos: `topics.name → Title`, `topics.user_id → CreatedBy`.
  - **Streaming + insert_many(batch=1000, ordered=False)**: ~30k registros
    en < 1 min.
  - **Índice único parcial** en votes `(IdeaId, UserId)` que ignora UserId
    no-string para replicar el comportamiento de NULL en MySQL.
  - **Idempotente** con `--reset`; confirmación interactiva `[s/N]` (o
    `--yes` para CI/CD).
  - **Log dual** stdout + `migration_log_YYYYMMDD_HHMMSS.txt`.