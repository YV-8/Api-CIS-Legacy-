# mysql_to_mongo.py — Herramienta MySQL → MongoDB

Script Python para verificar conectividad, migrar usuarios, verificar integridad y revertir
cambios entre MySQL y MongoDB en el microservicio `user-management-ms`.

---

## Requisitos previos

- Python 3.10 o superior
- Archivo `.env` configurado en la raíz del proyecto (ver `.env.example`)
- MySQL corriendo y accesible (con datos a migrar)
- MongoDB accesible (local o Atlas)

---

## Instalación

Desde la **raíz del proyecto**:

```bash
python3 -m venv scripts/mysql_to_mongo/.venv
source scripts/mysql_to_mongo/.venv/bin/activate
pip3 install -r scripts/mysql_to_mongo/requirements.txt
```

---

## Comandos disponibles

### 1. Verificar conectividad

Comprueba que el script puede conectarse a MySQL y a MongoDB antes de migrar:

```bash
python3 scripts/mysql_to_mongo/mysql_to_mongo.py check
```

### 2. Migrar

Copia todos los usuarios de MySQL a MongoDB. Incluye rollback automático ante cualquier fallo:

```bash
python3 scripts/mysql_to_mongo/mysql_to_mongo.py migrate
```

Migrar y verificar integridad automáticamente al finalizar:

```bash
python3 scripts/mysql_to_mongo/mysql_to_mongo.py migrate --verify
```

Ejecución de prueba (sin escribir nada en MongoDB):

```bash
python3 scripts/mysql_to_mongo/mysql_to_mongo.py migrate --dry-run
```

### 3. Verificar integridad

Compara los datos migrados en MongoDB contra los hashes calculados durante la migración
usando SHA-256 en tres niveles: hash global → hash por batch → comparación por campo.

```bash
python3 scripts/mysql_to_mongo/mysql_to_mongo.py verify
```

> Requiere que `migration_state.json` exista (se genera al finalizar `migrate`).
> El archivo es eliminado al ejecutar `rollback`.

**Niveles de verificación:**

| Nivel | Qué compara | Cuándo se ejecuta |
|-------|-------------|-------------------|
| 1 — Hash global | 1 string SHA-256 | Siempre |
| 2 — Hash por batch | ≤ N hashes de batch | Solo si Nivel 1 falla |
| 3 — Per-record | ≤ batch_size registros | Solo para batches fallidos |

**Códigos de salida:**

| Código | Significado |
|--------|-------------|
| `0` | Integridad verificada correctamente |
| `1` | Error de conexión o estado inválido |
| `3` | Integridad comprometida — se listan los IDs y campos afectados |

### 4. Rollback manual

Elimina **únicamente** los documentos que el script insertó en la última sesión.
Los documentos que ya existían en MongoDB antes de la migración **no se tocan**.

```bash
python3 scripts/mysql_to_mongo/mysql_to_mongo.py rollback
```

> El rollback lee el archivo `migration_state.json` que se genera automáticamente
> al final de cada migración exitosa.

---

## Flujo recomendado

```
check → migrate [--verify] → verify (en cualquier momento) → rollback (si falla)
```

---

## Desactivar el entorno virtual

```bash
deactivate
```

---

## Variables de entorno

El script lee automáticamente el `.env` de la raíz del proyecto.

| Variable | Descripción | Valor por defecto |
|----------|-------------|-------------------|
| `MYSQL_HOST` | Host de MySQL | `localhost` |
| `MYSQL_PORT` | Puerto de MySQL | `3307` |
| `DB_NAME` | Nombre de la base de datos MySQL | `sd3` |
| `DB_USERNAME` | Usuario de MySQL | *(requerido)* |
| `DB_PASSWORD` | Contraseña de MySQL | *(requerido)* |
| `MONGODB_URI` | URI de conexión a MongoDB | `mongodb://localhost:27017` |
| `MONGO_DB_NAME` | Nombre de la base de datos MongoDB | `sd3` |
| `MONGODB_TLS` | Habilita TLS en la conexión MongoDB (`true`/`false`) | `false` |
| `MYSQL_TLS` | Habilita TLS en la conexión MySQL (`true`/`false`) | `true` |

---

## Opciones adicionales

| Opción | Comando | Descripción |
|--------|---------|-------------|
| `--dry-run` | `migrate` | Lee MySQL pero no escribe en MongoDB |
| `--batch-size N` | `migrate` | Documentos por operación bulk (default: 500) |
| `--verify` | `migrate` | Ejecuta verificación de integridad al finalizar |
| `--verbose` / `-v` | todos | Activa logging DEBUG |
| `--mongo-uri` | todos | Sobreescribe `MONGODB_URI` |
| `--mongo-db` | todos | Sobreescribe `MONGO_DB_NAME` |
| `--mysql-host` | `check`, `migrate`, `verify` | Sobreescribe `MYSQL_HOST` |
| `--mysql-port` | `check`, `migrate`, `verify` | Sobreescribe `MYSQL_PORT` |
| `--mysql-user` | `check`, `migrate`, `verify` | Sobreescribe `DB_USERNAME` |
| `--mysql-password` | `check`, `migrate`, `verify` | Sobreescribe `DB_PASSWORD` |

---

## Comportamiento del rollback

| Tipo | Cuándo ocurre | Qué elimina |
|------|---------------|-------------|
| **Automático** | Al detectar un error durante `migrate` | Solo los docs insertados en esa sesión |
| **Manual** | Al ejecutar `rollback` después de un `migrate` exitoso | Solo los docs insertados en la última sesión |

Los documentos que ya existían en MongoDB **antes** de iniciar la migración nunca se eliminan.

---

## Modelo migrado

| MySQL (`users`) | MongoDB (`users`) | Tipo |
|-----------------|-------------------|------|
| `id` (VARCHAR UUID) | `_id` / `id` | String |
| `name` | `name` | String |
| `login` | `login` | String (índice único) |
| `password` | `password` | String |
| `role` (ENUM) | `role` | String |
