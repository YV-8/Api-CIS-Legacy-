# CIS API — Crowdsourced Ideation Solution

API REST desarrollada en **ASP.NET Core 10** para la plataforma de ideación colaborativa CIS (Fase 2).

---

## Estructura del proyecto

```
api-cis/
├── CIS.Api/            # Capa de presentación (controllers, configuración)
├── CIS.BusinessLogic/  # Capa de lógica de negocio
└── CIS.DataAcces/      # Capa de acceso a datos (DbContext, entidades)
```

## Tecnologías

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 9 con Pomelo (MySQL)
- Swashbuckle (Swagger / OpenAPI 3)
- Microsoft.Extensions.Diagnostics.HealthChecks

---

## Requisitos previos

- .NET 10 SDK
- MySQL 8.x en ejecución 

---

## Configuración

La cadena de conexión se configura en `CIS.Api/appsettings.Development.json` para desarrollo local:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3307;Database=sd3;User=...;Password=...;"
}
```

En producción se inyecta mediante la variable de entorno:

```
ConnectionStrings__DefaultConnection=Server=...;Database=...;User=...;Password=...;
```

Configuración JWT para validar tokens emitidos por `user-management-ms`:

```
Auth__Jwt__Secret=...
Auth__Jwt__Issuer=...
Auth__Jwt__Audience=...
Auth__Jwt__RequireIssuer=false
Auth__Jwt__RequireAudience=false
Auth__Jwt__ClockSkewSeconds=60
```

> `Auth__Jwt__Secret` es obligatorio y debe configurarse por entorno.

---

## Ejecutar la aplicación

```bash
dotnet run --project CIS.Api
```

La aplicación queda disponible en `http://localhost:5000`  o `https://localhost:7225`

---

## Endpoints base

| Método | Ruta       | Descripción                              |
|--------|------------|------------------------------------------|
| GET    | /health    | Verifica el estado de la app y la BD     |
| GET    | /swagger   | Documentación interactiva (solo en Dev)  |

### Ejemplo de respuesta `/health`

```json
{ "status": "healthy" }
```

---

## US-00 — Project Base Setup

Cambios implementados en esta historia de usuario:

- Corregida referencia rota en `CIS.BusinessLogic.csproj` (`CIS.AccesData` → `CIS.DataAcces`)
- Agregados paquetes NuGet: `Pomelo.EntityFrameworkCore.MySql`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
- Creado `CisDbContext` en `CIS.DataAcces/Data/`
- Configurada la cadena de conexión vía `appsettings.json` y variable de entorno
- Agregado endpoint `GET /health` con verificación de conectividad a MySQL
- Eliminados archivos de muestra (`WeatherForecast`)


Ejecución de tests

dotnet test CIS.Api.Tests/CIS.Api.Tests.csproj