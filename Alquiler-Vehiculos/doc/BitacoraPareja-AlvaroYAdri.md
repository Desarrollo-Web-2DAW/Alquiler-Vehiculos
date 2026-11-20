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
