# Rollback — US-24
 
## Requisitos
```bash
python3 -m venv venv
source venv/bin/activate
pip install pymongo python-dotenv
```
 
---
 
## Pasos para correr el rollback
 
```bash
# 1. Activar el entorno
source venv/bin/activate
 
# 2. Backup del estado actual
python scripts/rollback.py --backup-only
 
# 3. Rollback completo (deja MongoDB vacío)
python scripts/rollback.py --target 0 --yes
 
# 4. Restaurar desde el backup
python scripts/rollback.py --target latest --validate --yes
```
 
---
 
## Verificar en MongoDB
 
```bash
python -c "
import pymongo
db = pymongo.MongoClient('mongodb://localhost:27017')['sd3']
for col in ['users','topics','ideas','votes','comments']:
    print(f'{col}: {db[col].count_documents({})}')
"
```
 
**Resultado esperado:**
```
users: 7000
topics: 7000
ideas: 7000
votes: 7000
comments: 14000
```
 
---
 
## Verificar en MySQL
 
```bash
python -c "
import mysql.connector, os
from dotenv import load_dotenv
load_dotenv()
db = mysql.connector.connect(
    host=os.getenv('DB_HOST'),
    port=int(os.getenv('DB_PORT')),
    database=os.getenv('DB_NAME'),
    user=os.getenv('DB_USER'),
    password=os.getenv('DB_PASSWORD')
)
cur = db.cursor()
for tabla in ['users','topics','ideas','votes','comments']:
    cur.execute(f'SELECT COUNT(*) FROM {tabla}')
    print(f'{tabla}: {cur.fetchone()[0]}')
"
```
 
## Output
```
"============================================================",
        "CIS Platform - Rollback Script (US-24)",
        "============================================================",
        "  MongoDB:    sd3 @ mongodb://localhost:27017",
        "  Backup dir: backups",
        "  Log:        logs/rollback_20240501_143022.log",
        "  Target:     latest",
        "",
        "── Creando backup → backups/backup_20240501_143022.json ────",
        "  [users]    7000 documentos exportados",
        "  [topics]   7000 documentos exportados",
        "  [ideas]    7000 documentos exportados",
        "  [votes]    7000 documentos exportados",
        "  [comments] 14000 documentos exportados",
        "",
        "── Restaurando desde backup ─────────────────────────────────",
        "  Archivo: backups/backup_20240430_120000.json",
        "  [comments] 14000/14000 restaurados OK",
        "  [votes]     7000/7000  restaurados OK",
        "  [ideas]     7000/7000  restaurados OK",
        "  [topics]    7000/7000  restaurados OK",
        "  [users]     7000/7000  restaurados OK",
        "",
        "── Validacion post-restore ──────────────────────────────────",
        "  Coleccion      Backup      Mongo  Estado",
        "  ------------ ---------- ----------  ------",
        "  comments         14000      14000  OK",
        "  votes             7000       7000  OK",
        "  ideas             7000       7000  OK",
        "  topics            7000       7000  OK",
        "  users             7000       7000  OK",
        "",
        "Resultado: EXITOSO",
        "============================================================",
        "Rollback completado en 4.2s",
        "Resultado: EXITOSO",
        "Log guardado en: logs/rollback_20240501_143022.log",
        "============================================================",
```