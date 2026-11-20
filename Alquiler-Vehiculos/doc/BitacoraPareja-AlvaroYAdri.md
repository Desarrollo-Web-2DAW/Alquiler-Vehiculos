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
