# Seed Script — CIS Platform (US-27)

Script de inicialización de base de datos para la validación previa a la migración.
Genera datos realistas y referencialmente consistentes en las tablas `users`, `topics`, `ideas`, `votes` y `comments`.

---

## Requisitos previos

- Python 3.10 o superior
- Acceso a la instancia MySQL (`localhost:3307`, base de datos `sd3`)
- Ambos microservicios desplegados al menos una vez para que Hibernate/EF Core hayan creado las tablas

Verificar que Python está instalado:

```bash
python3 --version
```

---

## Instalación

### 1. Ir a la carpeta del script

```bash
cd /ruta/a/api-cis/seed
```

### 2. Crear un entorno virtual

```bash
python3 -m venv .venv
```

### 3. Activar el entorno virtual

```bash
source .venv/bin/activate
```

El prompt del terminal cambiará a `(.venv)` para indicar que está activo.

### 4. Instalar las dependencias

```bash
pip install -r requirements.txt
```

---

## Configuración

### 1. Crear el archivo `.env`

```bash
cp .env.example .env
```

### 2. Editar `.env` con las credenciales reales

```bash
nano .env
```

Contenido del archivo:

```env
# Conexión MySQL
DB_HOST=localhost
DB_PORT=3307
DB_NAME=sd3
DB_USER=tu_usuario
DB_PASSWORD=tu_contraseña

# Volumen de registros por tabla
USERS_COUNT=7000
TOPICS_COUNT=7000
IDEAS_COUNT=7000
VOTES_COUNT=7000
COMMENTS_COUNT=14000

# Tamaño del lote de inserción
BATCH_SIZE=500
```

> **Nota:** El archivo `.env` nunca debe subirse al repositorio. Está incluido en `.gitignore`.

---

## Ejecución

### Ejecución estándar (usa valores del `.env`)

```bash
python seed.py
```

### Ejecución con parámetros personalizados

Los argumentos de línea de comandos sobreescriben los valores del `.env`:

```bash
python seed.py --users 3000 --topics 3000 --ideas 3000 --votes 3000 --comments 6000
```

Parámetros disponibles:

| Parámetro | Descripción | Valor por defecto |
|---|---|---|
| `--host` | Host del servidor MySQL | `localhost` |
| `--port` | Puerto MySQL | `3307` |
| `--db` | Nombre de la base de datos | `sd3` |
| `--user` | Usuario MySQL | valor de `.env` |
| `--password` | Contraseña MySQL | valor de `.env` |
| `--users` | Cantidad de usuarios a generar | `7000` |
| `--topics` | Cantidad de topics a generar | `7000` |
| `--ideas` | Cantidad de ideas a generar | `7000` |
| `--votes` | Cantidad de votos a generar | `7000` |
| `--comments` | Cantidad de comentarios a generar | `14000` |
| `--batch` | Filas por sentencia INSERT | `500` |
| `--reset` | Limpia todas las tablas y vuelve a sembrar | `false` |
| `--clean-only` | Solo limpia las tablas sin resembrar datos | `false` |

### Limpiar y resembrar desde cero

> Esto elimina **todos** los datos existentes en las 5 tablas y luego los vuelve a insertar.

```bash
python seed.py --reset
```

### Solo limpiar sin resembrar

> Esto elimina **todos** los datos existentes en las 5 tablas sin volver a insertar nada.

```bash
python seed.py --clean-only
```

Ambos comandos piden confirmación antes de borrar:

```
ATENCION: --reset eliminara TODOS los datos existentes en las tablas:
  comments, votes, ideas, topics, users
Continuar? [s/N]
```

### Resumen de modos de ejecución

| Comando | Borra datos | Inserta datos |
|---|---|---|
| `python seed.py` | No | Solo si faltan registros |
| `python seed.py --reset` | Sí | Sí (todo desde cero) |
| `python seed.py --clean-only` | Sí | No |

---

## Salida esperada

```
============================================================
CIS Platform — Seed Script (US-27)
============================================================
  BD:           sd3 @ localhost:3307
  Usuarios:     7,000
  Topics:       7,000
  Ideas:        7,000
  Votos:        7,000
  Comentarios:  14,000
  Lote:         500

Generando hash de contraseña… 

── Usuarios ──────────────────────────────────────────
  [users] 7000/7000 (100%) 

── Topics ────────────────────────────────────────────
  [topics] 7000/7000 (100%) 

── Ideas ─────────────────────────────────────────────
  [ideas] 7000/7000 (100%) 

── Votos ─────────────────────────────────────────────
  [votes] 7000/7000 (100%) 
  [ideas.vote_count] Actualizando contadores… 

── Comentarios ───────────────────────────────────────
  [comments] 14000/14000 (100%) 

============================================================
  Seed completado en 87.3s (1.5 min)
============================================================
```

---

## Idempotencia

El script es seguro para ejecutarse múltiples veces. Antes de insertar en cada tabla verifica el conteo actual:

- Si ya existen **suficientes registros** (≥ objetivo), omite esa tabla.
- Para `users`, usa `INSERT IGNORE` respetando la restricción `UNIQUE` en la columna `login`.
- Para `votes`, respeta el índice único `(idea_id, user_id)`.

```
  [users] Ya existen 7000 registros — omitiendo.
  [topics] Ya existen 7000 registros — omitiendo.
  ...
```

---

## Datos generados

| Tabla | Volumen | Notas |
|---|---|---|
| `users` | 7.000 | Login único `usr_00001`…`usr_07000`, password `Seed@1234!` (bcrypt) |
| `topics` | 7.000 | Mix de tipos y estados; mayoría `active` para que stats funcionen |
| `ideas` | 7.000 | Distribuidas entre los topics generados; `deleted_at = NULL` |
| `votes` | 7.000 | Un voto por idea, usuario distinto; `vote_count` actualizado al final |
| `comments` | 14.000 | ~2 por idea; sin restricción de unicidad |

### Credenciales de los usuarios seed

| Campo | Valor |
|---|---|
| Login | `usr_00001` … `usr_07000` |
| Password | `Seed@1234!` |

---

## Verificación post-seed

Comprobar que la paginación retorna resultados correctos:

```bash
curl "http://localhost:5000/api/v1/topics?page=1&size=10"
```

Respuesta esperada: `totalItems: 7000`, `totalPages: 700`.

Comprobar que los stats retornan datos significativos:

```bash
curl "http://localhost:5000/api/v1/stats/top"
curl "http://localhost:5000/api/v1/stats/ideas/top"
curl "http://localhost:5000/api/v1/stats/users/top"
```

---

## Próximas ejecuciones

Una vez instalado el entorno virtual, solo se necesita activarlo:

```bash
cd /ruta/a/api-cis/seed
source .venv/bin/activate
python seed.py
```
