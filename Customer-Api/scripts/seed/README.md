# seed_users.py — Poblado de usuarios de prueba

Script Python configurable para insertar usuarios de prueba en la base de datos del microservicio `user-management-ms`. Soporta **MySQL** y **MongoDB**.

---

## Requisitos previos

- Python 3.10 o superior
- Archivo `.env` configurado en la raíz del proyecto (ver `.env.example`)
- Base de datos levantada (MySQL o MongoDB)

---

## 1. Crear el entorno virtual

Desde la **raíz del proyecto**, ejecutar:

```bash
python3 -m venv scripts/seed/.venv
```

## 2. Activar el entorno virtual

```bash
source scripts/seed/.venv/bin/activate
```

El prompt del terminal mostrará `(.venv)` cuando esté activo.

## 3. Instalar las dependencias

```bash
pip install -r scripts/seed/requirements.txt
```

---

## 4. Ejecutar el script

### Insertar 50 usuarios en MySQL (por defecto)

```bash
python scripts/seed/seed_users.py
```

### Insertar una cantidad específica de usuarios

```bash
python scripts/seed/seed_users.py --count 200
```

### Insertar usuarios en MongoDB

```bash
python scripts/seed/seed_users.py --db mongodb --count 100
```

---

## 5. Desactivar el entorno virtual

```bash
deactivate
```

---

## Variables de entorno

El script lee automáticamente el archivo `.env` de la raíz del proyecto.

| Variable | Descripción | Valor por defecto |
|----------|-------------|-------------------|
| `DB_HOST` | Host de MySQL | `localhost` |
| `DB_PORT` | Puerto de MySQL | `3307` |
| `DB_NAME` | Nombre de la base de datos | `sd3` |
| `DB_USERNAME` | Usuario de MySQL | `root` |
| `DB_PASSWORD` | Contraseña de MySQL | *(vacío)* |
| `MONGODB_URI` | URI de conexión a MongoDB | `mongodb://localhost:27017` |
| `MONGO_DB_NAME` | Nombre de la base de datos Mongo | `sd3` |

---

## Datos generados por usuario

| Campo | Descripción |
|-------|-------------|
| `id` | UUID generado aleatoriamente |
| `name` | Nombre completo en español (aleatorio) |
| `login` | Username único (máx. 20 caracteres) |
| `password` | Hash BCrypt de `Password1!` (compatible con Spring Security) |
| `role` | `USER` 70% · `OWNER` 20% · `ADMIN` 10% |

> La contraseña de todos los usuarios generados es **`Password1!`**

---

## Opciones disponibles

| Opción | Atajo | Descripción |
|--------|-------|-------------|
| `--count N` | `-n N` | Cantidad de usuarios a insertar |
| `--db mysql` | | Insertar en MySQL *(por defecto)* |
| `--db mongodb` | | Insertar en MongoDB |
