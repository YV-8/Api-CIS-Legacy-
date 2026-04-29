# mysql_to_mongo.py — Herramienta MySQL → MongoDB

Script Python para verificar conectividad, migrar usuarios y revertir cambios entre MySQL y MongoDB en el microservicio `user-management-ms`.

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

Ejecución de prueba (sin escribir nada en MongoDB):

```bash
python3 scripts/mysql_to_mongo/mysql_to_mongo.py migrate --dry-run
```

### 3. Rollback manual

Elimina **únicamente** los documentos que el script insertó en la última sesión. Los documentos que ya existían en MongoDB antes de la migración **no se tocan**.

```bash
python3 scripts/mysql_to_mongo/mysql_to_mongo.py rollback
```

> El rollback lee el archivo `migration_state.json` que se genera automáticamente al final de cada migración exitosa.

---

## Flujo recomendado

```
check → migrate → (si algo salió mal) rollback
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

---

## Opciones adicionales

| Opción | Comando | Descripción |
|--------|---------|-------------|
| `--dry-run` | `migrate` | Lee MySQL pero no escribe en MongoDB |
| `--batch-size N` | `migrate` | Documentos por operación bulk (default: 500) |
| `--verbose` / `-v` | todos | Activa logging DEBUG |
| `--mongo-uri` | todos | Sobreescribe `MONGODB_URI` |
| `--mongo-db` | todos | Sobreescribe `MONGO_DB_NAME` |
| `--mysql-host` | `check`, `migrate` | Sobreescribe `MYSQL_HOST` |
| `--mysql-port` | `check`, `migrate` | Sobreescribe `MYSQL_PORT` |
| `--mysql-user` | `check`, `migrate` | Sobreescribe `DB_USERNAME` |
| `--mysql-password` | `check`, `migrate` | Sobreescribe `DB_PASSWORD` |

---

## Comportamiento del rollback

| Tipo | Cuándo ocurre | Qué elimina |
|------|---------------|-------------|
| **Automático** | Al detectar un error durante `migrate` | Solo los docs insertados en esa sesión |
| **Manual** | Al ejecutar `rollback` después de un `migrate` exitoso | Solo los docs insertados en la última sesión |

Los documentos que ya existían en MongoDB **antes** de iniciar la migración nunca se eliminan en ningún caso.

---

## Modelo migrado

| MySQL (`users`) | MongoDB (`users`) | Tipo |
|-----------------|-------------------|------|
| `id` (VARCHAR UUID) | `_id` / `id` | String |
| `name` | `name` | String |
| `login` | `login` | String (índice único) |
| `password` | `password` | String |
| `role` (ENUM) | `role` | String |
