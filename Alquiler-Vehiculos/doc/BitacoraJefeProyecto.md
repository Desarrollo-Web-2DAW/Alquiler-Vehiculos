# Bitácora — Jefe de Proyecto (Antoine López)

## Tarea: Módulo de Usuarios, Autenticación/Autorización, Infraestructura Base, Docker y E2E (RF-13 · RF-14 · RF-15 · RF-46 · RI-03 · RNF-03 · RNF-04 · RNF-05 · RNF-09)

> **Fecha de registro:** 22/11/2026 · **Rama:** `feature/usuarios-fix` → `dev` · **Estado:** completado, verificado (206/206 tests + 16 peticiones Bruno E2E + Docker 4/4 healthy) y listo para integración en `dev`.

Este bloque consolida el núcleo de identidad, seguridad e infraestructura común del backend: gestión de cuentas con ASP.NET Core Identity y JWT, separación de perfiles de dominio (`Cliente` / `Trabajador`), administración de usuarios por `Admin`, modularización de `Program.cs`, contenedorización completa con Docker Compose y pruebas en las tres capas de la pirámide (unitarias, integración con Testcontainers y E2E con Bruno CLI).

---

### 1. Qué incluye

#### Identidad, autenticación y autorización (RF-46)
- **Entidades y persistencia EF Core (`AppDbContextUsuarios`):** separación entre cuenta de acceso `User : IdentityUser<Guid>` (tablas `usuarios`, `roles`, `user_roles`, etc.) y entidades de dominio `Cliente` y `Trabajador` enlazadas mediante claves foráneas opcionales (`ClienteId`, `TrabajadorId`).
- **Controlador de autenticación (`Controllers/Usuario/User/AuthController.cs`):**
  - `POST /api/auth/signup` → `201 Created` (alta pública de cliente + JWT).
  - `POST /api/auth/login` → `200 OK` (autenticación de cualquiera de los perfiles + JWT).
  - `GET /api/auth/me` → `200 OK` (perfil del usuario autenticado a partir del token).
- **Administración de usuarios (`Controllers/Usuario/User/UsersController.cs` + `IUserService`/`UserService`, solo `PolicyNames.Admin`):**
  - `GET /api/users` → `200 OK` (listado sin consultas N+1 sobre roles, RNF-02) y `GET /api/users/{id:guid}` → `200 OK` / `404`.
  - `PATCH /api/users/{id:guid}/role` → `200 OK` / `400` / `404` / `409` (valida compatibilidad persona ↔ rol `ROL_INCOMPATIBLE_CON_PERSONA` y protección del último admin `ULTIMO_ADMIN` en transacción).
  - `DELETE /api/users/{id:guid}` → `204 No Content` / `404` / `409` (baja lógica protegiendo `ULTIMO_ADMIN`) y `PATCH /api/users/{id:guid}/reactivate` → `200 OK` / `404`.
- **JWT y políticas (`Infrastructure/IdentityConfig.cs` y `PolicyNames.cs`):** firma HMAC-SHA256 con `ClockSkew = Zero`, claims propios (`cliente_id`, `trabajador_id`, `tipo_trabajador`) y 5 políticas con esquema explícito `JwtBearer`: `Admin`, `Responsable`, `Trabajador`, `Cliente` y `Mecanico` (que exige además el claim `tipo_trabajador=Mecanico` para RF-12).
- **Siembra idempotente (`IdentitySeeder.cs` · RI-03):** crea los 4 roles (`Cliente`, `Trabajador`, `Responsable`, `Admin`) y, en desarrollo, las 5 cuentas semilla completas (`Admin`, `Cliente`, `Trabajador` de Operaciones, `Mecanico` y `Responsable`) con DNIs válidos dentro de una transacción.

#### Reglas de negocio y seguridad (RF-13 · RF-14 · RF-15 · RNF-03 · RNF-09)
- **Alta transaccional de cliente (`AuthService.SignUpAsync`):** crea `Cliente`, `User` y rol `Cliente` en una única transacción de PostgreSQL; si falla cualquier paso ejecuta `RollbackAsync()` evitando clientes huérfanos.
- **Documentos de identificación (RF-13, RF-14, RF-15):** DNI, NIE y pasaporte son opcionales por separado pero obligatorios en conjunto (al menos uno). El campo `NumeroPermisoConducir` solo se conserva si el cliente se identifica con pasaporte (se normaliza a `null` cuando aporta DNI/NIE).
- **Cuentas inactivas y baja lógica:** tanto `LoginAsync` como `GetMeAsync` rechazan cuentas inactivas o dadas de baja lógica (`IsDeleted` / `!Activo`).
- **Secretos fuera del repositorio (RNF-03):** `Jwt:Key` eliminada de `appsettings.json`; se inyecta con `dotnet user-secrets` en local y variable de entorno `Jwt__Key` en Docker/producción, con validación *fail-fast* al arranque (mínimo 32 bytes / 256 bits).
- **Logs sin datos sensibles (RNF-09):** `SerilogConfig.cs` filtra ruido de EF Core/Npgsql y `AuthService`/`UserService` registran únicamente `UserId` y resultados, nunca correos ni contraseñas en claro.

#### Arquitectura e infraestructura (RNF-05)
- **Modularización de `Program.cs`:** configuración separada en `Infrastructure/SerilogConfig.cs`, `DatabaseConfig.cs`, `DatabaseInitializationExtensions.cs` e `IdentityConfig.cs`, reconectando en un único punto EF Core, Identity, controladores, validadores, caché Redis, notificaciones Email, Taller, `GlobalExceptionHandler` y `/health`.
- **Limpieza técnica:** eliminadas las 4 excepciones duplicadas en `Alquiler_Vehiculos.Exceptions.Usuario` (unificadas en `DomainExceptions.cs` para `GlobalExceptionHandler`) y alineados todos los paquetes NuGet de EF Core (`10.0.4`) y `Microsoft.IdentityModel.*` (`8.0.1`), dejando el build en **0 warnings**.
- **Dockerización completa (`Dockerfile`, `.dockerignore`, `.env.example`, `compose.yaml`):** imagen multi-stage (`sdk:10.0` → `aspnet:10.0`) con usuario no-root (`USER app`), permisos en `/app/logs` para Serilog, endpoint `/health` nativo y 4 servicios orquestados en red interna `alquiler-network`: `postgres` (`5433`), `redis` (`6379`), `mailpit` (`1025`/`8025`) y `api` (`8080`), todos con `healthcheck`.

---

### 2. Flujo de autenticación y autorización

```mermaid
sequenceDiagram
    actor C as Cliente / Bruno
    participant API as AuthController / UsersController
    participant V as FluentValidation
    participant S as AuthService / UserService (ROP)
    participant DB as PostgreSQL (EF Core + Identity)

    C->>API: POST /api/auth/signup o /login
    API->>V: Valida formato de entrada (400)
    API->>S: SignUpAsync / LoginAsync
    S->>DB: Transacción ACID (Cliente + User + Rol) / Verifica credenciales y estado
    alt Error de dominio (401 / 403 / 409)
        S-->>API: Result.Failure(DomainError)
        API-->>C: ToHttpResult() con código uniforme
    else Éxito
        S-->>API: Result.Success(AuthResponseDto + JWT con claims)
        API-->>C: 201 Created / 200 OK + Bearer JWT
    end
    C->>API: GET /api/users (Authorization: Bearer JWT)
    API->>API: JwtBearer valida firma y PolicyNames.Admin (stateless)
    alt Rol distinto de Admin
        API-->>C: 403 Forbidden
    else Rol Admin
        API->>S: GetAllAsync / CambiarRolAsync / DesactivarAsync
        S-->>API: Result (200 / 204 / 404 / 409)
        API-->>C: Respuesta HTTP
    end
```

---

### 3. Decisiones de diseño

**D1 — ASP.NET Core Identity con claves `Guid` frente a autenticación manual con `long`.**
- *Justificación:* Identity proporciona de serie *hashing* seguro con sal (PBKDF2), gestión de roles, unicidad de email y protección contra fuerza bruta (`Lockout`: 5 intentos / 5 min) en un componente auditado por Microsoft. La clave primaria `Guid` evita ataques de enumeración (IDOR) en URLs y no expone el número de usuarios registrados; en cuentas de usuario priorizamos seguridad frente a la pequeña ventaja de rendimiento de un `long` secuencial.
- *Alternativa descartada:* implementar autenticación manual con BCrypt y claves `long`, que aumenta la superficie de error en lógica crítica de seguridad sin aportar requisitos que Identity no cubra.

**D2 — Patrón `Result<T, DomainError>` (ROP) en servicios y controladores.**
- *Justificación:* lanzar excepciones para controlar flujos esperables de negocio (credenciales incorrectas, email duplicado, cuenta inactiva, último admin) es un antipatrón que ensucia las firmas y penaliza el rendimiento. Con `Result<T, DomainError>` los errores posibles son explícitos, se traducen uniformemente a HTTP mediante `ToHttpResult()` y resultan mucho más sencillos de probar en tests unitarios, dejando `GlobalExceptionHandler` solo para errores inesperados.
- *Alternativa descartada:* lanzar excepciones de negocio desde `AuthService`/`UserService` para capturarlas en middleware.

**D3 — Composición (`User` ↔ `Cliente` / `Trabajador`) y alta transaccional ACID en PostgreSQL.**
- *Justificación:* un `User` representa credenciales de acceso, mientras que `Cliente` y `Trabajador` son entidades de dominio con atributos y ciclos de vida distintos (un trabajador no es cliente con la misma cuenta, y un `Admin` no necesita perfil). Se opta por composición mediante FKs opcionales en lugar de herencia. Además, `SignUpAsync` y `CambiarRolAsync` envuelven las escrituras multi-tabla en transacciones ACID de PostgreSQL: si falla a mitad del proceso, el rollback evita dejar registros huérfanos o cuentas sin rol.
- *Alternativa descartada:* modelar herencia en una única tabla o persistir identidades en un almacén documental como MongoDB, menos adecuado para relaciones con claves foráneas y atomicidad multi-tabla.

**D4 — JWT *stateless* con claims de dominio y secretos fuera del repositorio (RNF-03).**
- *Justificación:* incrustar `cliente_id`, `trabajador_id` y `tipo_trabajador` en el JWT firmado permite que las políticas de autorización (como `PolicyNames.Admin` o `PolicyNames.Mecanico`) validen permisos en cada petición sin consultar la BD y sin riesgo de manipulación por el cliente. `Jwt:Key` se saca de `appsettings.json` hacia `user-secrets` (local) y `Jwt__Key` (Docker/prod), con comprobación *fail-fast* de mínimo 256 bits al arrancar.
- *Alternativa descartada:* almacenar `Jwt:Key` en `appsettings.json` o consultar la tabla de trabajadores en BD en cada petición protegida.

**D5 — Validación en dos niveles (`FluentValidation` + reglas en servicio) y regla RF-13 de documentos.**
- *Justificación:* `FluentValidation` centraliza de forma declarativa, rápida y reutilizable la validación de formato de entrada sin dependencias de BD (`SignupValidator`, `LoginValidator`, `CambiarRolValidator`), mientras que los servicios aplican las reglas que dependen del estado persistido (duplicados `409`, compatibilidad persona-rol `409`, protección del último admin `409`, cuenta activa `403`, y normalización de `NumeroPermisoConducir` a `null` cuando hay DNI/NIE).
- *Alternativa descartada:* validar todo manualmente con `if` en el controlador o mezclar comprobaciones sintácticas con consultas a BD.

**D6 — Estrategia de pruebas siguiendo la pirámide de testing + entorno Docker reproducible (RNF-04, RNF-05).**
- *Justificación:* cada capa cubre un nivel de riesgo con el coste adecuado: tests unitarios rápidos y baratos en la base (aislando servicios con `Moq`), integración en la capa intermedia contra **PostgreSQL real efímero con Testcontainers** y `WebApplicationFactory<Program>` (validando también el rechazo `403 Forbidden` entre roles reales), y E2E con **Bruno CLI** en la cúspide simulando a un usuario real tanto en `local` (`5216`) como en contenedor Docker `desarrollo` (`8080`). Los tests de Bruno generan email y DNI únicos por ejecución (`script:pre-request`) para ser repetibles sin reiniciar la BD.
- *Alternativa descartada:* depender solo de pruebas manuales en Postman/Swagger o sustituir PostgreSQL por EF Core `InMemory`, que no valida migraciones ni transacciones reales.

---

### 4. Pruebas y cobertura

**Pruebas del módulo de Usuarios / Auth (69 tests C# + 16 peticiones E2E Bruno):**

| Capa | Fichero | Tests | Qué verifica |
|---|---|---:|---|
| Unitaria | `Unit/Auth/AuthControllerTests.cs` | 7 | Mapeo HTTP de `Login`, `Signup` y `GetMe` (`200`, `201`, `401`, `403`, `409`) doblando `IAuthService` con `Moq` |
| Unitaria | `Unit/Users/UsersControllerTests.cs` | 8 | Mapeo HTTP de `GetAll`, `GetById`, `CambiarRol`, `Desactivar` y `Reactivar` (`200`, `204`, `404`, `409`) doblando `IUserService` con `Moq` |
| Unitaria | `Unit/Validators/LoginValidatorTests.cs` | 5 | Reglas de formato de email y contraseña obligatoria |
| Unitaria | `Unit/Validators/SignupValidatorTests.cs` | 20 | Formato de campos, política de contraseña, fecha de nacimiento y exigencia de al menos un documento (DNI/NIE/pasaporte) |
| Unitaria | `Unit/Validators/CambiarRolValidatorTests.cs` | 6 | Validación de los 4 roles permitidos, rol vacío y rol inexistente |
| Integración | `Integration/Auth/AuthServiceIntegrationTests.cs` | 11 | Flujos de `SignUpAsync`, `LoginAsync` y `GetMeAsync` contra PostgreSQL real |
| Integración | `Integration/Auth/AuthIntegrationTests.cs` | 3 | Pipeline HTTP completo con `WebApplicationFactory<Program>` + Testcontainers (`postgres:17-alpine`): `GetMe` sin token (`401`), baja lógica (`403`) y flujo `Signup` → `GetMe` (`201` + `200`) |
| Integración | `Integration/Auth/UsersIntegrationTests.cs` | 9 | Autorización real por roles (`401` sin token, `403` para `Cliente`/`Trabajador`/`Mecanico`/`Responsable`, `200` `Admin`), cambio de rol válido (`200`) e incompatible (`409`), baja lógica (`204`), reactivación (`200`) y protección `ULTIMO_ADMIN` (`409`) |
| E2E | `Alquiler-Vehiculos.Tests.E2E/Bruno-Cli/` (`1 - AUTHENTICATION` + `2 - USERS`) | 16 (27 checks) | Flujo completo de autenticación (5 perfiles semilla + signup repetible + `/me`) y administración `/api/users` (`401`, `403` Cliente/Trabajador, `200` Admin, `409 ULTIMO_ADMIN`) en `--env local` y `--env desarrollo` |

**Verificación global de la rama:**
- `dotnet build` → **0 Warning(s), 0 Error(s)**.
- `dotnet test Alquiler-Vehiculos/Alquiler-Vehiculos.slnx` → **206/206 en verde** (incluida la corrección de la condición de carrera en `EmailSmtpIntegrationTests` al esperar el asunto `REF-E2E` en Mailpit).
- `docker compose up -d --build` → **4/4 contenedores `healthy`** (`api`, `postgres`, `redis`, `mailpit`) y `GET http://localhost:8080/health` → `200 OK Healthy`.
- `bru run --env local` y `bru run --env desarrollo` → colección E2E repetible en ambos entornos.

---

### 5. Requisitos cubiertos

| Requisito | Descripción | Estado |
|---|---|---|
| **RF-13** | Alta de cliente con datos personales y regla de permiso de conducir solo con pasaporte | Implementado |
| **RF-14 / RF-15** | Identificación mediante DNI, NIE o pasaporte (independientes, al menos uno obligatorio) | Implementado (formato de letra pendiente de enlazar al fusionar Clientes) |
| **RF-46** | Autenticación JWT, autorización por roles/políticas (`401`/`403`) y gestión administrativa de cuentas (`/api/users`) | Implementado |
| **RI-03** | Semilla inicial idempotente de roles y 5 perfiles de prueba en desarrollo | Implementado |
| **RNF-03** | Secretos (`Jwt:Key`, credenciales SMTP) fuera del repositorio (`user-secrets` / `.env`) | Implementado |
| **RNF-04** | Pruebas automatizadas en 3 capas (unitarias, integración con Testcontainers y E2E con Bruno CLI) | Implementado |
| **RNF-05** | Entorno reproducible con un solo comando (`Dockerfile` + `compose.yaml` + `/health`) | Implementado |
| **RNF-09** | Registro estructurado con Serilog sin volcar datos personales en claro | Implementado |

---

### 6. Pendientes y coordinación con el resto del equipo

- **Integración con Clientes (`feature/clientes/controlador`):** conectar los validadores de letra oficial de DNI/NIE/pasaporte en `SignupValidator` y alinear código HTTP de menor de edad (`400`, RF-13).
- **Integración con Vehículos (`feature/Vehiculo/seed`):** unificar `DbContext` (evitar duplicar `AppDbContextUsuarios`) y contrato de `DomainError` antes del merge a `dev`.
- **Oficina única (RF-11):** necesaria antes de validar horarios de recogida/devolución en el bloque de Reservas (CU-18, CU-22).
