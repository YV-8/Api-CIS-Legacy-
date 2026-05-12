# Contrato de Autenticacion entre `user-management-ms` y `api-cis` (Fase 0)

## 1. Objetivo

Definir un contrato tecnico versionado para que `api-cis` valide tokens emitidos por `user-management-ms` de forma consistente, segura y trazable.

## 2. Alcance

- **Emision de token:** `user-management-ms`
- **Consumo y validacion de token:** `api-cis`
- **Transporte:** header HTTP `Authorization: Bearer <token>`
- **Version de contrato:** `v1`

## 3. Proveedor de identidad (autoridad)

- **Servicio autoridad:** `user-management-ms`
- **Endpoint de autenticacion vigente:** `POST /v1/auth/login`
- **Respuesta esperada minima:**
  - `token` (JWT)

## 4. Especificacion del JWT (v1)

### 4.1 Algoritmo y firma

- **Algoritmo:** `HS256`
- **Clave:** secreto compartido por entorno (no hardcodeado en codigo fuente)
- **Fuente de configuracion:** variables de entorno/secret manager

> Nota: como mejora de seguridad posterior se migrara a firma asimetrica (`RS256`) o `JWKS`.

### 4.2 Claims obligatorios

- `sub`: identificador de usuario autenticado (en `user-management-ms` corresponde al login)
- `role`: rol del usuario (ej. `ADMIN`, `OWNER`, `USER`)
- `iat`: fecha/hora de emision
- `exp`: fecha/hora de expiracion

### 4.3 Claims opcionales recomendados

- `iss`: issuer (autoridad emisora)
- `aud`: audiencia objetivo (servicios consumidores)

## 5. Reglas de validacion en `api-cis`

- Validar firma JWT con la clave del entorno correspondiente.
- Rechazar token expirado (`exp`).
- Requerir `sub` y `role` como claims minimos para endpoints protegidos.
- Si se define `iss`, validar `iss` exacto.
- Si se define `aud`, validar `aud` exacto.
- Tolerancia de reloj (`clock skew`) maxima recomendada: 60 segundos.

## 6. Mapeo de identidad para CIS

- `CurrentUserId` en `api-cis` se resolvera desde:
  1. `sub` (principal)
  2. fallback temporal: `ClaimTypes.NameIdentifier` si aplica en compatibilidad

Regla de largo plazo: usar solo `sub` para evitar ambiguedades.

## 7. Modelo de autorizacion (roles)

Para Fase 0 se acuerda estandarizar rol en claim `role` con valores en mayusculas:

- `OWNER`
- `ADMIN`
- `USER`

La politica por endpoint se detallara en Fase 1, pero se acuerda que cualquier endpoint de escritura en `api-cis` requiere autenticacion valida.

## 8. Errores estandar de autenticacion/autorizacion

`api-cis` respondera con:

- `401 Unauthorized` cuando el token falta, es invalido o esta expirado.
- `403 Forbidden` cuando el token es valido pero no cumple permisos requeridos.

Formato de error se unificara mediante middleware global en Fase 1.

## 9. Configuracion por entorno

Variables minimas recomendadas:

- `Auth__Jwt__Secret`
- `Auth__Jwt__Issuer` (si aplica)
- `Auth__Jwt__Audience` (si aplica)
- `Auth__Jwt__RequireIssuer` (`true/false`)
- `Auth__Jwt__RequireAudience` (`true/false`)

No se permiten secretos en repositorio ni en `appsettings` versionado.

## 10. Pruebas de contrato (DoD de Fase 0)

Se considera Fase 0 completada cuando:

1. Existe este contrato versionado y aprobado por ambos equipos.
2. Se prueba emision de token desde `user-management-ms`.
3. Se valida manualmente que el token contiene `sub`, `role`, `iat`, `exp`.
4. Se acuerda matriz de valores por entorno (dev/qa/prod) para secret, issuer y audience.
5. Se registra el plan de migracion de `HS256` a esquema asimetrico.

## 11. Decisiones abiertas para Fase 1

- Definir valor final de `iss` y `aud`.
- Definir si `sub` sera login permanente o id tecnico de usuario.
- Definir politica de rotacion de secretos.
- Definir fecha objetivo de migracion a `RS256/JWKS`.
