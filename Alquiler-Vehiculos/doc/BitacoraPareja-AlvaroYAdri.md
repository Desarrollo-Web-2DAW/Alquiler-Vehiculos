# Bitácora — Pareja Álvaro y Adri

## Tarea: caché LRU con TTL sobre Redis

> **Fecha de registro:** 17/11/2026 · **Rama:** `feature/cache` · **Estado:** diseño cerrado, pendiente de implementación.

### Alcance y requisitos que cubre

| Requisito | Relación con la caché |
|---|---|
| **RNF-02 (rendimiento)** | Caché de las búsquedas de disponibilidad con **invalidación cuando cambien reservas o estados**, sin cargas N+1 y respuesta habitual < 1 s en local. Es el requisito que sostiene esta tarea. |
| **CU-15 (ampliación)** | La invalidación de la caché al cancelar una reserva (el "último coche") encaja directamente en el diseño de invalidación selectiva. |
| **RF-22 (pago con tarjeta)** | Restricción inamovible: **los pagos jamás se cachean** (ni en la caché ni en las estadísticas). Regla que se respeta en todo el diseño. |

---

## 1. Investigación: organización de la caché

Pautas de organización estudiadas para una caché en una API .NET:

- La caché vive en una **carpeta propia `Cache/`** dentro del proyecto API (hermana de `Controllers`, `Services`, `Models`...), con namespace `...Cache`. **No** va dentro de `Infrastructure`:
  - `Cache/ICacheService.cs` — contrato de la caché.
  - `Cache/CacheOptions.cs` — opciones (`SectionName = "Cache"`).
  - `Cache/MemoryCacheService.cs` — implementación en memoria.
  - `Cache/RedisCacheService.cs` — implementación sobre Redis (`IDistributedCache` + JSON).
- El registro en DI va aparte, en `Infrastructures/CacheConfig.cs`, como extensión `AddCache(...)`.
- Configuración en `appsettings.json`: sección `"Cache"` (`DefaultExpirationMinutes`, `SlidingExpirationMinutes`) y sección `"Redis"` (`ConnectionString`).
- Contrato de `ICacheService`: `GetAsync<T>`, `SetAsync<T>(key, value, TimeSpan? expiration, ct)`, `RemoveAsync`, `ExistsAsync`, `GetOrSetAsync(key, factory, expiration, ct)` — todo asíncrono con `CancellationToken`.
- Los tests (NUnit + FluentAssertions + Moq) viven en `Tests/Unit/Services/Cache/`, con nombres en español (`GetAsync_CacheMiss_RetornaNull`) y comentarios "Dado que... cuando... entonces".

**Coincidencia notable:** el `CacheOptions` de referencia ya combina expiración absoluta (`DefaultExpirationMinutes`) y deslizante (`SlidingExpirationMinutes`), que es exactamente el TTL mixto que hemos decidido para esta tarea.

---

## 2. Decisiones tomadas

Cada decisión con su justificación y la alternativa descartada, como exige el enunciado.

**D1 — Caché única sobre Redis, sin distinción de entornos.**
- *Justificación:* un único mecanismo de caché para todo el sistema, con el mismo comportamiento en cualquier entorno, sin mantener ni probar dos implementaciones equivalentes.
- *Alternativa descartada:* doble implementación (memoria en desarrollo + Redis en producción), por la complejidad de dos adaptadores con semánticas equivalentes y dos juegos de pruebas.
- *Declarado:* Redis es servicio obligatorio también en desarrollo; el `docker compose` de un solo comando (RNF-05) lo incluye.

**D2 — TTL mixto: vida máxima absoluta + renovación en lectura.**
- *Justificación:* lo muy consultado se renueva sin penalizar, pero con techo: nada vive más de la vida máxima, así la disponibilidad obsoleta tiene fecha de caducidad garantizada.
- *Alternativas descartadas:* absoluto puro (caduca datos frescos en uso) y deslizante puro (sin techo: un dato consultado mucho podría vivir indefinidamente obsoleto).
- *Implicación técnica:* Redis solo tiene TTL absoluto nativo. El techo de vida máxima se guarda en el valor (fecha de creación / deadline) y se comprueba al leer; la renovación es un `EXPIRE` por lectura, nunca más allá del deadline. La renovación se puede limitar en frecuencia como optimización documentada.

**D3 — Poda con timer en segundo plano.**
- *Justificación:* mantiene limpio el índice de recencia/etiquetas, que Redis no limpia por su cuenta (borra claves caducadas, pero no los miembros del índice que apuntan a ellas).
- *Alternativa descartada:* solo limpieza perezosa (al leer y al escribir), porque el índice de recencia puede quedar con entradas huérfanas sin acceso que nadie limpie.
- *Implicación:* el barrido queda como método invocable a mano y el timer solo lo periódicamente, para probarlo sin esperar reloj (criterio de RF-40). Periodo configurable.

**D4 — Invalidación selectiva por etiquetas cuando cambien reservas o estados.**
- *Justificación:* cumple RNF-02 (y CU-15) sin tirar entradas sanas; la invalidación es explícita y fina, y "solo las claves afectadas" por cambio de reserva o de estado de vehículo.
- *Alternativa descartada:* `Clear()` total ante cualquier cambio, que degrada el acierto con cambios frecuentes.
- *Implicación:* cada entrada se asocia a etiquetas (`vehiculo:{id}`, `categoria:{categoria}`, `matricula:{matricula}`) mediante sets auxiliares en Redis; la interfaz expone `Remove`, `RemoveByTag` y `Clear`. Pendiente de acordar con el equipo de reservas la tabla **evento → etiquetas invalidadas** (reserva creada/cancelada/caducada, cambio de estado de vehículo...). El enunciado no se ve afectado: RNF-02 se cumple y de forma más fina, CU-15 encaja directo y RF-22 se mantiene (pagos nunca en caché).

**D5 — La política LRU es nuestra, implementada sobre Redis.**
- *Justificación:* la tarea es implementar una LRU con TTL; así la capacidad es un número de entradas nuestro, medible y determinista, y la expulsión es demostrable en pruebas. Redis aporta almacén, atomicidad y borrado de claves caducadas.
- *Alternativa descartada:* delegar la LRU en `maxmemory-policy` de Redis: casi no habría código que implementar y la "capacidad" sería memoria del servidor, no una LRU propia.
- *Implicación:* el orden de acceso se mantiene en un sorted set (miembro = clave, score = marca de acceso) y al superar la capacidad se expulsan las menos usadas. Como la expulsión toca varios sitios (clave, índice, etiquetas), debe ser atómica (script Lua o transacción) — coherente con la concurrencia obligatoria de RF-30.

**D6 — La caché vive en una carpeta propia `Cache/`.**
- *Justificación:* da a la caché un lugar identificable, hermano de `Controllers`, `Services`, `Models`... El resto del código no depende de su interior, solo de la interfaz.
- *Alternativa descartada:* una carpeta dentro de `Infrastructure`, por petición explícita del equipo y porque separaría el registro del resto de la pieza.

**D7 — Contrato `ICacheService` con nombres y estilo convencionales.**
- *Justificación:* mantener los mismos nombres y estilo (`GetAsync`, `SetAsync`, `RemoveAsync`, `ExistsAsync`, `GetOrSetAsync`, asíncrono con `CancellationToken`) facilita que el resto del equipo reconozca la pieza. Sobre esa base se añaden las operaciones propias de esta tarea: `RemoveByTag` y `Clear` (invalidación selectiva) y la configuración de capacidad LRU en las opciones.
- *Alternativa descartada:* inventar un contrato nuevo sin relación con el de la sección 1, que obligaría a re-aprender la interfaz y complicaría la integración.

---

## 3. Diseño en Redis (boceto)

Cuatro espacios de nombres bajo un prefijo común (`cache:`):

- **Valores:** `cache:val:{clave}` — JSON con envolvente (`CacheEnvelope`: valor serializado + deadline absoluto del TTL mixto). TTL físico = ventana móvil, recortada al deadline.
- **Índice de recencia (LRU):** `cache:lru` (sorted set) — miembro = clave, score = marca de acceso (hora en ms).
- **Índice de etiquetas:** `cache:tag:{etiqueta}` (set de claves) — base de la invalidación selectiva.
- **Etiquetas por clave:** `cache:tags:{clave}` (set de etiquetas) — para limpiar el índice al expulsar o borrar.

Operaciones (contrato `ICacheService`, implementación `LruCacheService` sobre `ICacheStore`):

- `GetAsync` / `GetOrSetAsync`: comprueba el techo del TTL mixto, renueva TTL móvil y score; si no está, se calcula y se inserta.
- `SetAsync`: escribe, indexa en LRU y etiquetas, y expulsa las menos usadas si se supera la capacidad (script Lua atómico: guarda, indexa y expulsa en una sola operación).
- `RemoveAsync`, `RemoveByTagAsync`, `ClearAsync`: invalidación manual selectiva o total (scripts Lua atómicos).
- `PruneAsync` (invocable a mano en pruebas, la lanza el timer `CachePruneService`): limpia del índice los miembros cuya clave ya caducó.

---

## 4. Estructura en el proyecto (implementada)

```
Alquiler-Vehiculos.Back/
  Cache/                          ← carpeta propia (convención de la sección 1)
    ICacheService.cs              contrato de la caché (Get/Set/Exists/GetOrSet/Remove/RemoveByTag/Clear/Prune)
    ICacheStore.cs                abstracción del almacén (puerto para Redis y para el fake de pruebas)
    CacheOptions.cs               capacidad, ventana móvil, vida máxima, periodo de poda
    CachePolicy.cs                política pura: TTL mixto y cálculo de expulsión LRU
    LruCacheService.cs            servicio de caché (envolvente, política, índice)
    RedisCacheStore.cs            almacén sobre Redis con scripts Lua atómicos
    CachePruneService.cs          timer de poda (invocable a mano en pruebas, RF-40)
    CacheConfig.cs                registro en DI (extensión AddCache)
Alquiler-Vehiculos.Tests/
  Unit/Cache/                     CachePolicyTests, LruCacheServiceTests, FakeCacheStore, FakeTimeProvider
  Integration/Cache/RedisCacheStoreTests.cs   contra Redis de verdad (se omiten sin Redis)
```

Configuración en `appsettings.json` con el siguiente seccionado:

```
"Cache":  { "Capacity": 500, "SlidingExpirationSeconds": 60, "AbsoluteMaxLifetimeSeconds": 600, "PruneIntervalSeconds": 60 }
"Redis":  { "ConnectionString": "localhost:6379" }
```

---

## 5. Plan de trabajo

1. ~~Investigar la convención de caché y fijar decisiones~~ (hecho).
2. ~~Estructura, contrato y opciones~~ (hecho).
3. ~~Núcleo LRU + TTL mixto sobre Redis (envolvente, índice de recencia, expulsión atómica, etiquetas, poda, reloj inyectable)~~ (hecho).
4. ~~Pruebas: unitarias de política pura + integración con Redis~~ (hecho; las de integración se omiten sin Redis).
5. ~~Registro en DI + opciones configurables~~ (hecho).
6. Acordar con el grupo la tabla **evento → etiquetas invalidadas** (D4) y las decisiones pendientes del apartado 6.
7. Integración con el servicio de disponibilidad y los cambios de reserva/estado (cuando existan) — tarjeta aparte.
8. Cerrar tarjeta en el Kanban y actualizar esta bitácora con el resultado.

## 6. Registro de implementación (17/11/2026)

Decisiones tomadas durante la implementación, sobre las del apartado 2:

- **D8 — Cliente Redis: StackExchange.Redis 3.4.35** (estándar de facto). *Alternativa descartada:* `IDistributedCache` de Microsoft, porque la política LRU propia (D5) necesita operar sobre sorted sets y sets, que esa abstracción no expone.
- **D9 — Reloj inyectable: `TimeProvider`** (estándar de .NET), con `FakeTimeProvider` en las pruebas para mover el tiempo sin esperas (criterio de RF-40). *Alternativa descartada:* un `IClock` propio, por no reinventar lo que ya trae la plataforma.
- **D10 — Abstracción `ICacheStore`** (puerto) con `RedisCacheStore` (adaptador real, scripts Lua) y `FakeCacheStore` (adaptador falso en memoria para las unitarias). *Justificación:* separa la política testeable de los scripts Lua, que solo se pueden validar contra Redis de verdad; sigue el patrón de puertos y adaptadores. *Alternativa descartada:* mockear `IDatabase` de StackExchange.Redis, enorme y poco manejable.
- **D11 — TTL inicial de la entrada = ventana móvil** (y no la vida máxima), que es la semántica estándar de "absoluto + deslizante": sin accesos, la entrada caduca a los 60 s; con accesos, se renueva hasta el techo de 600 s.
- **D12 — Pruebas con NUnit puro** (sin FluentAssertions ni Moq): menos dependencias y el `FakeCacheStore` sustituye a los mocks. *Alternativa descartada:* incorporar ambos paquetes; queda como decisión de grupo si se quiere estandarizar después.

Estado de las pruebas (`dotnet test`): **Total: 27 · OK: 22 · KO: 0 · Omitidas: 5** (las de integración, que necesitan Redis; se omiten solas si no hay uno disponible y se ejecutan con `TEST_REDIS_CONNECTION` o `localhost:6379`). Requisito cubierto por las pruebas: **RNF-02** (y la invalidación selectiva que usará CU-15).

## 7. Pendientes

- Tabla **evento → etiquetas invalidadas** (D4) — acordar con el equipo de reservas.
- Añadir el servicio Redis al `docker compose` (RNF-05) cuando se monte; en desarrollo `Redis:ConnectionString` apunta a `localhost:6379`.
- Decide el grupo si se incorporan FluentAssertions y Moq (D12) y la ubicación definitiva de `CacheConfig.cs` (aquí, dentro de `Cache/`).
- Aviso heredado de la plantilla: NuGet reporta vulnerabilidad alta en `Microsoft.OpenApi` 2.0.0 (dependencia de `Microsoft.AspNetCore.OpenApi` 10.0.1). Conviene actualizar el paquete cuando el grupo toque la API.

## 8. Revisión de tests y cobertura (20/11/2026)

Revisión del código y de las pruebas según las convenciones de calidad del proyecto y el enunciado de Práctica 4.

**Tests alineados con las convenciones de testing del proyecto:**

- Comentarios `// Arrange`, `// Act` y `// Assert` en todos los casos (patrón AAA).
- Aserciones con **FluentAssertions** (`Should()`): se adopta la librería que quedó pendiente en D12. Las excepciones se comprueban con `Action` / `Func<Task>` y `Should().Throw*`.
- Atributos `Category("Unit")` y `Category("Integration")` para ejecutar por filtros.
- Nombres `Método_Escenario_ResultadoEsperado` y `#region` por método (convención del proyecto).
- Dobles a mano (`FakeCacheStore`, `CacheStub`) sin Moq: no se mockea lo que se puede probar con objetos reales.

**Cambios aplicados:**

- `RedisCacheStoreTests` reescrito con **Testcontainers** (`Testcontainers.Redis` 4.17.0, imagen `redis:7.4-alpine`): el contenedor se arranca en `OneTimeSetUp` y se libera en `OneTimeTearDown`. Se retira el `Assert.Ignore` que omitía las pruebas sin Redis: si falta Docker, el bloqueo queda a la vista y no se oculta.
- Tests nuevos: `CacheConfigTests` (registro en DI y enlace de opciones), `CachePruneServiceTests` (poda periódica, resiliencia ante fallos y cancelación limpia al detener, RF-40) y casos nuevos de `LruCacheService` (Remove, deadline agotado con valor físico vivo, vida máxima propia, factory null, dependencias null).
- `CachePolicyTests` y `LruCacheServiceTests` reescritos al nuevo estilo.
- Código: eliminados `TagKey` y `TagsOfKey` de `RedisCacheStore` (helpers sin uso, detectados en la revisión) y `Microsoft.AspNetCore.OpenApi` actualizado a 10.0.15, que resuelve el aviso del apartado 7. Borrado `UnitTest1.cs` (template de Visual Studio sin uso).
- Dependencias de pruebas nuevas: `FluentAssertions` 8.11.0 y `Testcontainers.Redis` 4.17.0 (módulo de Redis actual, con su constructor de imagen, que sustituye al obsoleto).

**Resultados (`dotnet test`):**

- Unitarias de caché: **35/35 OK**.
- Suite completa con integración (Redis de verdad en contenedor): **43/43 OK · 0 omitidas**.
- Cobertura del módulo `Cache/` (`dotnet test --collect:"XPlat Code Coverage"`): **181/181 líneas (100 %)**.

**Resoluciones y nuevos pendientes:**

- Resuelto: decisión D12 (FluentAssertions adoptado; Moq no hace falta) y el aviso de `Microsoft.OpenApi` (actualizado el paquete).
- Siguen abiertos: tabla **evento → etiquetas invalidadas** (D4), servicio Redis en el `compose.yaml` (RNF-05) y la integración con el servicio de disponibilidad.
- Nuevo pendiente: decidir si el contrato `ICacheService` mantiene sus adiciones (`ExistsAsync`, `GetOrSetAsync`, `RemoveByTagAsync`, `ClearAsync`, `PruneAsync` y `CancellationToken`) frente al contrato base (solo `Get`/`Set`/`Remove` sin token), y citar la decisión en la memoria.

---

## Tarea: notificaciones por correo electrónico

> **Fecha de registro:** 18/11/2026 · **Rama:** `feature/notifications/Mail` · **Estado:** módulo implementado y documentado, pendiente de pruebas e integración con los servicios del grupo.

### Alcance y requisitos que cubre

| Requisito | Relación con el correo |
|---|---|
| **RF-34** | Correos en los pasos importantes de la reserva: los 8 eventos de la tabla del enunciado (alta, pago, caducidad, cancelación, recogida, devolución, reembolso y arreglo de taller) |
| **RF-35** | El envío **nunca bloquea ni rompe la reserva**: el correo se encola y, si el servidor falla, el error queda registrado para reintentarlo |
| **RF-36** | En desarrollo se ve el correo sin salir de la máquina (Mailpit); en producción apunta a un servidor real con credenciales en configuración |
| **CU-17 (ampliación)** | Correo caído durante una racha de pagos: cada cliente recibe **exactamente un** aviso (ni cero ni dos) |
| **CU-12, CU-23, CU-24 (acuerdos de equipo)** | Avisos al cliente por avería durante el alquiler, vehículo no listo y devolución tardía |
| **RNF-09** | Registros con `ILogger` sin datos sensibles: solo tipo de evento y código de referencia, nunca el correo del destinatario |

---

## 1. Investigación: envío de correos en la API

- **Implementaciones de ejemplo estudiadas**: una con `IEmailService` + `MailKitEmailService` (MailKit/MimeKit, con `ISmtpClient` inyectable para pruebas), `MemoryEmailService` (mock con `SentEmails`), `EmailQueueService` (`ConcurrentQueue` + `SemaphoreSlim`) y `EmailBackgroundWorker` (`BackgroundService` que dequeúa y envía), con su configuración SMTP tipada; otra con el mismo esqueleto pero con `Channel<T>` en vez de cola manual, plantillas HTML generadas desde C#, pipeline **Polly** (retry + circuit breaker + timeout) y registro de servicios por entornos.
- **Carencias comunes** que nuestro diseño resuelve: la cola vive solo en memoria (se pierde al reiniciar la app), un fallo de envío se traga el mensaje sin reintentos, no hay control de duplicados (ni cero ni dos no está garantizado) y en desarrollo solo se loguea el correo en vez de poder *verlo*.

## 2. Decisiones tomadas

Cada decisión con su justificación y la alternativa descartada (versión completa con diagramas en `doc/Notificaciones-Email-Memoria.md`).

**D1 — Patrón *outbox*: el correo se encola y lo envía un worker en segundo plano.**
- *Justificación:* RF-35 exige que el correo nunca bloquee la reserva y que el error quede registrado para reintentarlo. El outbox deja constancia del mensaje y su estado (`Pendiente`, `Enviado`, `Fallido`).
- *Alternativas descartadas:* envío directo desde el servicio de negocio (un SMTP caído retrasa o rompe la petición) y cola solo en memoria tipo `Channel`/`ConcurrentQueue` (los mensajes se pierden al reiniciar).

**D2 — Clave de idempotencia por evento (`{Tipo}:{CodigoReferencia}`).**
- *Justificación:* CU-17 pide "ni cero ni dos"; ante reinicios o dobles llamadas, el segundo encolado con la misma clave se descarta.
- *Alternativa descartada:* confiar en que cada evento se notifica una sola vez.

**D3 — Reintentos con backoff exponencial (30 s · 60 s · 120 s…, máx. 5) en el worker.**
- *Justificación:* cumple RF-35 sin saturar un servidor caído; al agotar intentos el mensaje queda `Fallido` y auditable.
- *Alternativa descartada:* Polly con *circuit breaker* (reintentos con apertura de circuito), válida como ampliación pero añade dependencia para lo que el worker resuelve con pocas líneas.

**D4 — Cuenta SMTP real del proyecto como envío activo, con Mailpit disponible para desarrollo.**
- *Justificación:* el equipo decide enviar con la cuenta real del proyecto; RF-36 pide además *ver* el correo en desarrollo sin salir de la máquina, y Mailpit (contenedor en `compose.yaml`, SMTP 1025 y bandeja web 8025) cubre ese escenario como alternativa comentada en la configuración de desarrollo.
- *Alternativas descartadas:* `MemoryEmailService` que solo loguea y MailHog (abandonado).

**D5 — Plantillas HTML como ficheros con marcadores `{{Propiedad}}` y bloques `{{#each}}`.**
- *Justificación:* el equipo pidió plantillas en HTML, CSS, JavaScript y Bootstrap; como ficheros se editan y previsualizan sin recompilar.
- *Alternativas descartadas:* HTML embebido en strings de C# y motores tipo Razor/Scriban (dependencia extra para condicionales que no necesitamos).

**D6 — MailKit para SMTP.**
- *Alternativa descartada:* `System.Net.Mail.SmtpClient` (obsoleto) y proveedores externos tipo SendGrid (atan el proyecto a un servicio de pago).

**D7 — `IEmailOutboxStore` con implementación en memoria, lista para EF Core.**
- *Justificación:* la BBDD transaccional no está decidida; al fijar EF Core solo se añade una implementación nueva (tabla con índice único sobre `ClaveIdempotencia`) sin tocar el resto.
- *Alternativa descartada:* EF Core desde el día uno, que depende de una decisión que no es nuestra.

**D8 — Configuración tipada `EmailSettings` (sección `Email`) con secretos fuera del repositorio.**
- *Justificación:* RNF-03; la contraseña va en *user-secrets* en desarrollo y en la variable de entorno `Email__Password` en producción, nunca en el repositorio. `Email:UseSmtp=false` conmuta al servicio simulado para pruebas.
- *Alternativa descartada:* cadenas sueltas y credenciales en el código.

## 3. Diseño del flujo (boceto)

Evento de negocio → DTO de notificación (`Dto/Email`) → plantilla renderizada → mensaje en la cola (outbox, con clave de idempotencia) → `EmailBackgroundWorker` (cada 10 s toma los pendientes cuyo reintento vence) → `MailKitEmailService` → Mailpit/SMTP. Los tres diagramas de secuencia (flujo normal, reintento y CU-17) están en `doc/Notificaciones-Email-Memoria.md`, validados con un parser real de Mermaid.

## 4. Estructura en el proyecto (implementada)

```
Alquiler-Vehiculos.Back/
  Models/Email/        TipoNotificacion, EstadoEmailOutbox, EmailOutboxMessage, TiposEventos
  Dto/Email/           NotificacionEmailBase + un DTO por evento + LineaEmailDto
  Services/Email/      IEmailNotifier/EmailNotifier (nunca lanza excepción),
                       IEmailTemplateRenderer/EmailTemplateRenderer,
                       IEmailOutboxStore/InMemoryEmailOutboxStore,
                       EmailBackgroundWorker,
                       IEmailService/MailKitEmailService/MemoryEmailService
  Infrastructure/      EmailSettings (config tipada) + EmailConfig (registro DI)
  Templates/Email/     11 plantillas HTML (una por evento) + assets/email.css
compose.yaml           Mailpit: SMTP 1025 y bandeja web 8025
doc/Notificaciones-Email-Memoria.md   documentación para la memoria
```

## 5. Plan de trabajo

1. ~~Investigar y comparar implementaciones de envío de correo de ejemplo~~ (hecho).
2. ~~Contratos y DTOs de los 11 eventos~~ (hecho).
3. ~~Renderizador de plantillas y plantillas HTML con Bootstrap~~ (hecho).
4. ~~Cola outbox con idempotencia + worker con reintentos~~ (hecho).
5. ~~Configuración por entornos, Mailpit y registro en DI~~ (hecho).
6. ~~Documentación para la memoria (diagramas y decisiones)~~ (hecho).
7. Pruebas: unitarias de render/notifier/worker e integración con Mailpit (pendiente).
8. Integración con los servicios de reserva, pago, taller y tareas programadas cuando existan (pendiente, depende del grupo).

## 6. Registro de implementación (18/11/2026)

- Paquete **MailKit 4.18.2**: se descartó la 4.13.0 porque NuGet reporta vulnerabilidades conocidas en MailKit/MimeKit de esa línea.
- Resuelto el aviso heredado que quedaba en el apartado 7 de la tarea de caché: `Microsoft.AspNetCore.OpenApi` actualizado a 10.0.15, que elimina la vulnerabilidad de `Microsoft.OpenApi` 2.0.0. La solución compila con **0 errores y 0 advertencias**.
- Diagramas Mermaid de la memoria revisados con un parser real: se corrigieron dos fallos (paréntesis en etiqueta de flecha del diagrama de arquitectura y una declaración de `participant` sin la palabra clave en el diagrama de secuencia).
- Merge de `origin/dev` (módulo de caché) en esta rama: conflictos resueltos combinando ambos módulos en `Program.cs` (registros `AddCache` y `AddEmailNotifications`), `appsettings*.json` (secciones `Email`, `Cache` y `Redis`) y `.csproj` (paquetes MailKit, StackExchange.Redis y OpenApi).

**Configuración con cuenta real (20/11/2026):** el envío se configura con la cuenta del proyecto (`funkopopapi@gmail.com`) por SMTP con TLS (`smtp.gmail.com:587`). La contraseña de aplicación queda **fuera del repositorio** (RNF-03): *user-secrets* en desarrollo y variable de entorno `Email__Password` en producción. Se añade `appsettings.Production.json` para la configuración por entornos (RNF-05) y se mantiene comentada la alternativa de Mailpit para la captura local de RF-36. No se registran credenciales en el código, en la configuración versionada ni en esta bitácora.

## 7. Pendientes

- ~~Pruebas del módulo (la estrategia está tabulada en `doc/Notificaciones-Email-Memoria.md`, apartado 7)~~ (hecho: ver apartado 8).
- Pasar `IEmailOutboxStore` a EF Core cuando el grupo fije la BBDD transaccional.
- Acordar con el grupo los puntos de llamada: **evento → método de `IEmailNotifier`** (una línea por evento; tabla en la memoria).
- Decisión a documentar: los clientes de correo reales (Outlook, Gmail) no soportan Bootstrap ni JavaScript; si se exige envío a buzones reales, generar variante con estilos *inline*.
- Añadir los servicios de Mailpit y Redis al `docker compose` definitivo (RNF-05): hoy hay `compose.yaml` solo con Mailpit.

## 8. Pruebas y cobertura (20/11/2026)

Pruebas del módulo de correo siguiendo la convención de pruebas del proyecto: NUnit + FluentAssertions, patrón AAA con sus comentarios (`// Arrange`, `// Act`, `// Assert`), `Category("Unit")` y `Category("Integration")`, nombres `Método_Escenario_Resultado`, dobles a mano salvo donde hace falta un doble de interfaz (Moq con su `Verify`) e integración con servicios reales en contenedor.

- **Unitarias (40, todas OK)**: renderizador de plantillas (marcadores, listas, formatos y las 11 plantillas), notificador (los 11 eventos, idempotencia y resiliencia ante fallos), worker (envío, reintentos con backoff, agotamiento de intentos y cancelación), cola outbox, servicio simulado, envío SMTP (autenticación, modos de seguridad y propagación de fallos), DTOs y registro en el contenedor.
- **Integración (2, OK) contra Mailpit en contenedor** con Testcontainers: envío real con MailKit por el camino de producción (sin cliente simulado) y flujo completo notificador → cola → worker verificado en la bandeja, incluida la comprobación de no duplicados de CU-17.
- **Cobertura del módulo de correo**: **365/365 líneas (100 %)** con `dotnet test --collect:"XPlat Code Coverage"`.

Requisitos cubiertos por las pruebas: **RF-34**, **RF-35**, **RF-36** y **CU-17** (y los avisos de CU-12, CU-23 y CU-24 a través de sus plantillas).

---

## Tarea: middleware de gestión global de excepciones

_**Fecha de registro:**_ 20/11/2026 · _**Rama:**_ `feature/middleware` · _**Estado:**_ implementado, registrado y probado.

### Alcance y objetivo

Centralizar la gestión de excepciones no controladas en la API para evitar respuestas inconsistentes y proporcionar información uniforme sobre los errores.

---

## 1. Implementación

Se ha creado `Middleware/GlobalExceptionHandler.cs`, que contiene:

- `GlobalExceptionHandler`: middleware encargado de capturar las excepciones durante el procesamiento de las peticiones HTTP.
- `GlobalExceptionHandlerExtensions`: clase de extensión para facilitar su registro en el pipeline de ASP.NET Core.

**Funcionamiento:**

- Captura las excepciones mediante `try/catch`.
- Registra los errores utilizando `ILogger`, generando un identificador único (`errorId`) para facilitar su seguimiento.
- Clasifica las excepciones y asigna el código HTTP correspondiente:
  - `NotFoundException` → 404.
  - `ValidationException` → 400, incluyendo los errores por campo.
  - `BusinessException` → 400.
  - `UnauthorizedAccessException` → 401.
  - `ArgumentException` → 400.
  - `TimeoutException` → 408.
  - Cualquier otra excepción → 500.
- Devuelve una respuesta JSON con `errorId`, `message`, `errorType`, `timestamp`, `path`, `method` y, cuando corresponda, `errors`.
- Configura la serialización JSON para utilizar nombres `camelCase` y omitir propiedades nulas.

## 2. Registro en Program.cs

Se ha registrado el middleware inmediatamente después de construir la aplicación:

```csharp
var app = builder.Build();

app.UseGlobalExceptionHandler();
```

La extensión `UseGlobalExceptionHandler()` permite incorporar el middleware al pipeline sin utilizar directamente `UseMiddleware` en `Program.cs`.

## 3. Prueba de funcionamiento

- Se creó temporalmente el endpoint `/test-exception` para provocar una excepción no controlada.
- Se comprobó que el middleware captura la excepción y devuelve una respuesta JSON con código HTTP 500.
- Se verificó la generación del identificador del error y el resto de información de la respuesta.
- Se ajustó `path` para devolver directamente la ruta como cadena mediante `context.Request.Path.Value`.

**Resultado:** implementación, registro y prueba del `GlobalExceptionHandler` completados correctamente.

## 4. Alcance de la tarea

Esta tarea comprende exclusivamente el middleware de gestión global de excepciones (`GlobalExceptionHandler`). No incluye la implementación de `SecurityHeadersMiddleware` ni `RateLimitMiddleware`.

---

## Tarea: partes de taller

_**Fecha de registro:**_ 22/11/2026 · _**Rama:**_ `feature/taller` · _**Estado:**_ núcleo del parte implementado, probado y documentado; las integraciones con vehículos y reservas esperan a esos módulos.

### Alcance y requisitos que cubre

| Requisito | Relación con el taller |
|---|---|
| **RF-08** | Partes de taller: cada entrada al taller abre un parte con `Id`, `Fecha`, `QuienLoTramita`, `Descripcion`, `Vehiculo` y `Estado` (`Abierto`/`Cerrado`); solo se cierra al terminar la reparación y no se reabren. Es un documento interno: los clientes no lo ven (RF-46) |
| **RF-12** | Solo un trabajador de tipo `Mecanico` (y activo) abre y cierra partes |
| **RF-01** | Consulta `TienePartesAbiertosAsync`: la comprobación de «sin partes abiertos» antes de dar de baja un vehículo (si no, 409) |
| **CU-07** | Ida y vuelta del taller: abrir parte, meter el vehículo en `Mantenimiento` y devolverlo al día siguiente cerrando el parte (la transición del vehículo llega con su módulo) |
| **CU-08** | Vehículos en taller con su fecha de entrada: partes abiertos ordenados por fecha |

### Implementación

- `Models/Taller/ParteTaller` + `Enums/Taller/EstadoParte`: modelo con los campos del enunciado y máquina de estados mínima (`Abierto` → `Cerrado`, sin reabrir: cada entrada al taller es un parte nuevo).
- `Services/Taller/ParteTallerService` con patrón `Result<T, DomainError>` y los errores de dominio del patrón del proyecto (`ConflictError` 409, `ForbiddenError` 403, `BusinessRuleError` 422, `NotFoundError` 404). Orden de comprobaciones: permiso del tramitador → datos → reglas de negocio.
- `Repositories/Taller`: contrato `IParteTallerRepository` con implementación en memoria, donde la regla «un solo parte abierto por vehículo» es atómica. Cuando se fije la base de datos transaccional, la implementación de EF Core la garantizará con un índice único parcial (igual que la idempotencia del outbox de correo).
- `Validators/Taller/AbrirParteValidator` (FluentValidation): identificador de vehículo de 11 caracteres (RF-01) y descripción obligatoria con límite de longitud.
- Registro en el contenedor con `Infrastructure/TallerConfig.AddTallerServices()`: una línea en `Program.cs`.
- `Errors/Taller/ParteTallerError`: códigos estables `PARTE_NO_ENCONTRADA`, `PARTE_ABIERTA_EXISTENTE`, `PARTE_YA_CERRADA` y `SOLO_MECANICO`.

### Decisiones

- **Relación con el vehículo:** el parte guarda su identificador corto de 11 caracteres (RF-01); la clave foránea y las transiciones `Disponible` ↔ `Mantenimiento` se cablean cuando se fusione el módulo de vehículos. Así el núcleo del taller no depende de ramas en curso.
- **Procedimiento de taller con reservas afectadas (RF-07)** documentado en `doc/Partes-Taller-Memoria.md` con la prioridad propuesta: reasignar a un vehículo equivalente de la misma categoría → aplazar el arreglo → cancelar con aviso y reembolso. Su implementación espera al servicio de reservas; los correos de aviso ya están implementados en el módulo de notificaciones.

### Pruebas y cobertura

- **38 pruebas unitarias** (servicio, repositorio, validador y registro en DI) siguiendo las convenciones del proyecto: NUnit + FluentAssertions, patrón AAA con sus comentarios, `Category("Unit")`, nombres `Método_Escenario_Resultado` y dobles a mano.
- **Cobertura del módulo de taller: 211/211 líneas (100 %)** con `dotnet test --collect:"XPlat Code Coverage"`.
- Suite completa de la rama: **96/96 OK · 0 omitidas**.

---

# Tarea: notificaciones en tiempo real con SignalR

**Fecha de registro:** 23/11/2026 · **Rama:** `feature/notifications/signalr` · **Estado:** infraestructura de SignalR implementada y conexión con los eventos de taller integrada; pendientes los emisores dependientes de Reservas y Vehículos y las pruebas específicas.

### Alcance y requisitos que cubre

| Requisito | Relación con SignalR |
|---|---|
| **RF-37** | Notificaciones en tiempo real para el personal: reserva creada, cambio de estado de reserva, vehículo disponible, reserva caducada y entrada/salida de vehículos del taller. |
| **RF-38** | Caducidad automática de reservas pendientes de pago cada minuto. SignalR deberá comunicar el evento de reserva caducada cuando exista la tarea de negocio correspondiente. |
| **RF-46** | El personal de oficina puede visualizar los eventos en tiempo real. El acceso se restringe a Trabajador, Responsable y Admin. |

La implementación se centra en la infraestructura de comunicación, la autorización, la conexión desde la aplicación cliente y la integración con los eventos de taller que ya tienen operaciones de negocio.

Los eventos de reservas y disponibilidad de vehículos quedan preparados para conectarse cuando estén terminados esos módulos. No se crean servicios ni operaciones ficticias para adelantarse a su implementación.

---

## 1. Investigación: organización de SignalR

He utilizado SignalR de ASP.NET Core para implementar la comunicación en tiempo real entre la API y la aplicación cliente Razor Pages.

La solución se organiza en torno a estos elementos:

- **Hub (`NotificacionesHub`)**: punto de conexión al que se conectan los clientes autorizados.
- **Grupo de destinatarios**: agrupa las conexiones del personal de oficina para enviar una notificación a todos sus integrantes sin tener que gestionar individualmente cada conexión.
- **Contexto del Hub (`IHubContext`)**: permite enviar eventos desde los servicios de la aplicación sin depender directamente de una conexión concreta.
- **Extensión de envío (`NotificacionesHubExtensions`)**: centraliza el envío de notificaciones al grupo de trabajadores.
- **Catálogo de eventos (`NotificacionEventos`)**: mantiene los nombres de los eventos en un único lugar para evitar cadenas duplicadas y errores tipográficos.

La comunicación se realiza desde el backend hacia los clientes conectados. No se exponen métodos del Hub para que los clientes puedan incorporarse arbitrariamente a grupos.

---

## 2. Decisiones tomadas

Cada decisión responde a una necesidad concreta del proyecto y evita introducir dependencias innecesarias.

**D1 — Un único Hub para las notificaciones del personal.**

- *Justificación:* todos los eventos de RF-37 están dirigidos al personal de oficina. Un único Hub simplifica la conexión, la autorización y el mantenimiento.
- *Alternativa descartada:* crear un Hub independiente para cada tipo de notificación, porque dividiría una funcionalidad que comparte destinatarios y configuración.

**D2 — Grupo común `trabajadores-oficina`.**

- *Justificación:* permite distribuir cada evento a todos los trabajadores de oficina conectados mediante `Clients.Group(...)`.
- *Implementación:* al establecer una conexión, `NotificacionesHub.OnConnectedAsync()` incorpora automáticamente la conexión al grupo.
- *Alternativa descartada:* enviar individualmente a cada usuario o gestionar manualmente la pertenencia desde el cliente.

**D3 — Autorización mediante la política existente `PolicyNames.Trabajador`.**

- *Justificación:* la política ya admite los roles Trabajador, Responsable y Admin, que son los destinatarios de las notificaciones.
- *Implementación:* el Hub utiliza `[Authorize(Policy = PolicyNames.Trabajador)]`.
- *Alternativa descartada:* crear una política específica duplicando los mismos roles o permitir conexiones sin autenticar.

**D4 — Autenticación JWT para la conexión SignalR.**

- *Justificación:* el backend ya utiliza JWT Bearer y la aplicación cliente utiliza autenticación mediante cookies. Se necesita trasladar el token JWT de la sesión autenticada al cliente SignalR sin exponerlo en el código HTML.
- *Implementación:* se ha creado el endpoint protegido `/auth/signalr-token` en Clients, que obtiene el JWT del claim `access_token` de la identidad autenticada. El endpoint responde con `Cache-Control: no-store`.
- *Alternativa descartada:* almacenar el JWT en almacenamiento accesible desde JavaScript o incluirlo directamente en la página.

**D5 — Transporte Long Polling.**

- *Justificación:* se ha utilizado `HttpTransportType.LongPolling` para la conexión actual, que permite enviar el token mediante la cabecera de autorización.
- *Implicación:* no ha sido necesario añadir `OnMessageReceived` a la configuración general de JWT para extraer `access_token` de la URL. Si se cambia a WebSockets o Server-Sent Events, habrá que revisar ese mecanismo de autenticación.
- *Alternativa descartada:* modificar innecesariamente la configuración general de autenticación del proyecto para una necesidad que el transporte actual no presenta.

**D6 — Configuración CORS específica para SignalR.**

- *Justificación:* el frontend y la API funcionan en orígenes distintos durante el desarrollo. La conexión necesita permitir las credenciales y las cabeceras correspondientes.
- *Implementación:* política `SignalR`, con el origen `http://localhost:5232`, `AllowAnyHeader()`, `AllowAnyMethod()` y `AllowCredentials()`.
- *Implicación:* la política debe ejecutarse antes de autenticación y autorización en el pipeline HTTP para que las peticiones preflight no reciban un 401.

**D7 — Catálogo centralizado de nombres de eventos.**

- *Justificación:* evita que los servicios y el cliente utilicen nombres diferentes para un mismo evento.
- *Implementación:* clase estática `NotificacionEventos` con las seis constantes de RF-37.
- *Alternativa descartada:* mantener los nombres como cadenas independientes en cada servicio.

**D8 — Reconexión automática y reintento de la conexión inicial.**

- *Justificación:* `withAutomaticReconnect()` gestiona las desconexiones posteriores a una conexión establecida, pero no reintenta por sí solo un fallo inicial de `connection.start()`.
- *Implementación:* se combina la reconexión automática de SignalR con un reintento manual cada cinco segundos cuando falla la conexión inicial o se agotan los intentos automáticos.
- *Alternativa descartada:* confiar exclusivamente en `withAutomaticReconnect()`, porque dejaría sin cubrir los fallos de conexión iniciales.

**D9 — URL del Hub obtenida desde configuración.**

- *Justificación:* evitar direcciones del backend escritas directamente en JavaScript y facilitar los cambios de entorno.
- *Implementación:* Razor Pages obtiene `Api:BaseUrl` mediante `IConfiguration`, construye la ruta `/hubs/notificaciones` y la expone mediante el atributo `data-hub-url`.
- *Alternativa descartada:* mantener `http://localhost:5216/hubs/notificaciones` fija en el archivo JavaScript.

**D10 — Envío de eventos desde los servicios mediante `IHubContext`.**

- *Justificación:* los servicios de negocio pueden publicar notificaciones sin depender del ciclo de vida de una conexión SignalR.
- *Implementación:* se utiliza `IHubContext<NotificacionesHub>` y una extensión genérica para enviar eventos al grupo de trabajadores.
- *Alternativa descartada:* introducir referencias directas al Hub dentro de los servicios o enviar desde los controladores.

---

## 3. Diseño del flujo

Flujo de conexión:

1. El trabajador inicia sesión en Clients.
2. La aplicación obtiene el JWT almacenado en el claim `access_token` de la cookie autenticada.
3. SignalR solicita `/auth/signalr-token` mediante `accessTokenFactory`.
4. El cliente establece la conexión con `/hubs/notificaciones`.
5. El backend valida el JWT y la política `PolicyNames.Trabajador`.
6. `NotificacionesHub.OnConnectedAsync()` incorpora la conexión a `trabajadores-oficina`.
7. Cuando un servicio de negocio genera un evento, utiliza `IHubContext` para distribuirlo al grupo.
8. El JavaScript recibe el evento y lo presenta en la página de notificaciones.

Flujo de una notificación:

**Operación de negocio → persistencia correcta → `IHubContext` → grupo `trabajadores-oficina` → evento SignalR → cliente Razor Pages → notificación visual.**

Los eventos de taller se integran después de la operación correspondiente. Los emisores de reservas y disponibilidad se incorporarán cuando existan las operaciones de negocio definitivas.

---

## 4. Estructura en el proyecto

```text
Alquiler-Vehiculos.Back/
  Realtime/
    NotificacionesHub.cs
    NotificacionesHubExtensions.cs
    NotificacionEventos.cs

  Infrastructure/
    SignalRExtensions.cs

  Services/
    Taller/
      ParteTallerService.cs

Alquiler-Vehiculos.Clients/
  Pages/
    Notificaciones.cshtml
    Notificaciones.cshtml.cs

  wwwroot/
    js/
      notificaciones.js
    css/
      notificaciones.css
```

### Responsabilidad de cada archivo

- `NotificacionesHub.cs`: Hub protegido por la política de trabajadores y asignación automática al grupo de oficina.
- `NotificacionesHubExtensions.cs`: extensión genérica `SendToTrabajadoresAsync<T>()` para enviar notificaciones a todos los integrantes del grupo.
- `NotificacionEventos.cs`: catálogo de los seis nombres de evento.
- `SignalRExtensions.cs`: registro de SignalR, configuración de CORS y opciones del Hub, además de la extensión para mapear el endpoint.
- `Notificaciones.cshtml`: página Razor que presenta el estado de conexión y el contenedor de notificaciones.
- `notificaciones.js`: obtención del token, conexión, reconexión y recepción de eventos.
- `notificaciones.css`: presentación visual de las notificaciones.

---

## 5. Implementación

### 5.1. Hub y grupo de destinatarios

Se ha creado `NotificacionesHub`, protegido mediante:

```csharp
[Authorize(Policy = PolicyNames.Trabajador)]
```

El Hub incorpora automáticamente cada conexión autenticada al grupo:

```csharp
public const string GrupoTrabajadoresOficina = "trabajadores-oficina";
```

La incorporación se realiza en `OnConnectedAsync()`, sin permitir que el cliente elija libremente a qué grupo incorporarse.

### 5.2. Extensión para enviar notificaciones

Se ha implementado `SendToTrabajadoresAsync<T>()`, que utiliza:

```csharp
hubContext.Clients
    .Group(NotificacionesHub.GrupoTrabajadoresOficina)
    .SendAsync(nombreEvento, datos, cancellationToken);
```

De esta forma, cualquier servicio puede publicar una notificación utilizando el nombre del evento y sus datos, sin conocer los detalles de las conexiones activas.

### 5.3. Catálogo de eventos

Se han definido los seis eventos previstos en RF-37:

| Constante | Nombre enviado |
|---|---|
| `ReservaCreada` | `ReservaCreada` |
| `EstadoReservaCambiado` | `EstadoReservaCambiado` |
| `VehiculoDisponible` | `VehiculoDisponible` |
| `ReservaCaducada` | `ReservaCaducada` |
| `VehiculoEntradaTaller` | `VehiculoEntradaTaller` |
| `VehiculoSalidaTaller` | `VehiculoSalidaTaller` |

Los nombres se mantienen centralizados para que los emisores y receptores utilicen el mismo identificador.

### 5.4. Configuración y registro

En `SignalRExtensions.cs` se ha implementado:

- Registro de SignalR mediante `AddSignalR()`.
- Configuración de `EnableDetailedErrors = true`.
- Tamaño máximo de mensaje de 4 KB.
- Intervalo de mantenimiento de conexión de 15 segundos.
- Política CORS específica.
- Mapeo del Hub en `/hubs/notificaciones`.

En `Program.cs` se han incorporado:

```csharp
builder.Services.AddRealtimeSignalR();
```

Y, en el pipeline:

```csharp
app.UseCors(SignalRExtensions.CorsPolicy);
```

antes de autenticación y autorización.

Finalmente, se mapea el Hub:

```csharp
app.MapSignalRHubs();
```

### 5.5. Integración con autenticación en Clients

Se ha añadido el endpoint protegido `/auth/signalr-token`, que obtiene el JWT desde el claim `access_token` de la sesión autenticada.

La página de notificaciones utiliza `IConfiguration` para obtener `Api:BaseUrl` y construir la dirección del Hub. Así, el JavaScript no necesita conocer la dirección concreta de la API.

La conexión se realiza con el cliente oficial de SignalR para JavaScript, cargado desde CDN.

### 5.6. Reconexión

El cliente incorpora:

- Estado visual de conexión: conectando, conectado, reconectando y desconectado.
- `withAutomaticReconnect()` para recuperar conexiones interrumpidas.
- Reintentos manuales cada cinco segundos cuando falla la conexión inicial.
- Reintentos manuales cuando se agotan los intentos automáticos.

La conexión comprueba que se encuentra en estado `Disconnected` antes de iniciar un nuevo intento.

### 5.7. Integración de los eventos de taller

Se ha integrado SignalR en `ParteTallerService`, utilizando `IHubContext<NotificacionesHub>`.

- `AbrirParteAsync`: después de persistir correctamente el parte, emite `VehiculoEntradaTaller`.
- `CerrarParteAsync`: después de actualizar correctamente el parte, emite `VehiculoSalidaTaller`.

Los datos enviados contienen:

- `VehiculoId`.
- `Descripcion`.
- `Fecha`.

No se incluyen datos personales del trabajador.

En el cliente se reciben ambos eventos y se muestran con un título, una descripción y la fecha correspondiente.

**Límite funcional:** cerrar un parte no implica por sí mismo cambiar el estado del vehículo a Disponible. Esa transición pertenece a la integración con el módulo de vehículos y no se ha inventado ni añadido al servicio de taller.

---

## 6. Registro de implementación (23/11/2026)

- Se implementó el Hub `NotificacionesHub`, protegido por `PolicyNames.Trabajador`.
- Se creó el grupo `trabajadores-oficina` y la incorporación automática de conexiones autenticadas.
- Se creó la extensión genérica `SendToTrabajadoresAsync<T>()`.
- Se centralizaron los seis nombres de evento en `NotificacionEventos`.
- Se registró SignalR en el backend mediante `AddRealtimeSignalR()` y se mapeó `/hubs/notificaciones`.
- Se configuró CORS para el origen de Clients y se corrigió el orden del pipeline, colocando `UseCors()` antes de autenticación y autorización.
- Se añadió el endpoint `/auth/signalr-token` en Clients para proporcionar el JWT a SignalR sin incluirlo en el HTML.
- Se creó la página `Notificaciones.cshtml` y sus recursos CSS y JavaScript.
- Se implementó la recepción visual de los eventos de entrada y salida del taller.
- Se añadió la reconexión automática y el mecanismo de reintento manual para fallos de conexión iniciales.
- Se sustituyó la URL fija del Hub en JavaScript por una dirección construida desde `Api:BaseUrl` en Razor Pages.
- Se comprobó que la conexión llegaba al estado `Conectado`.
- Se comprobó la compilación del proyecto Backend y, tras el cambio de URL configurable, la compilación de Clients también terminó correctamente.
- Se comprobó el registro de `AddRealtimeSignalR()` junto a `AddTallerServices()` en la configuración de pruebas del taller.

**Resultado de compilación comprobado:**

- Backend: compilación correcta.
- Clients: compilación correcta, `net10.0`, sin errores ni advertencias en la salida compartida.

---

## 7. Pendientes e integración con el grupo

La infraestructura de comunicación está implementada. Quedan pendientes las integraciones que necesitan operaciones de negocio todavía no disponibles.

| Evento | Integración pendiente |
|---|---|
| `ReservaCreada` | Emisión desde la operación real de creación de reservas. |
| `EstadoReservaCambiado` | Emisión desde la operación real que modifica el estado de una reserva. |
| `ReservaCaducada` | Emisión desde la tarea programada de RF-38 cuando caduque una reserva. |
| `VehiculoDisponible` | Emisión desde la operación real que restablezca la disponibilidad del vehículo. |
| `VehiculoEntradaTaller` | Integrado en `AbrirParteAsync`. |
| `VehiculoSalidaTaller` | Integrado en `CerrarParteAsync`. |

**Pendientes concretos:**

- Integrar los cuatro emisores cuando estén terminados los módulos de Reservas y Vehículos.
- Acordar los datos definitivos de cada evento con sus respectivas operaciones de negocio, sin inventar DTOs ni propiedades antes de conocerlas.
- Completar la integración de RF-38 con la tarea de caducidad de reservas.
- Revisar el tratamiento de fallos de envío de SignalR para que una notificación no convierta una operación ya persistida en un falso fallo.
- Realizar pruebas específicas de SignalR, separadas de las pruebas unitarias de Taller.
- Comprobar la recepción de los seis eventos de extremo a extremo cuando estén disponibles todos los emisores.

**Alcance respetado:** no se han creado servicios, repositorios, controladores ni operaciones ficticias de reservas o vehículos. Tampoco se han modificado las configuraciones generales de autenticación de otros compañeros para resolver necesidades que no exige el transporte actual.

La integración final de los emisores se realizará cuando estén disponibles las operaciones reales, manteniendo esta infraestructura común de SignalR.