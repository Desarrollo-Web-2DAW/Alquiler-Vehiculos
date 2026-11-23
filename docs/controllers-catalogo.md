# Controladores de marcas, concesionarios y fotos

## Referencias y adaptación

- [UD02, 7.8: Result en controladores](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/07-excepciones-patron-result.md#78-integración-con-controladores): `Match`, `ActionResult<T>` y `ToHttpResult<T>()`.
- [Ejemplo de almacenamiento de productos](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/18-ProductosFileStorage/ProductosTest/Controllers/ProductosController.cs): constructor primario, formulario de archivo y `CreatedAtAction`.
- [UD02, 20.6 y 20.7](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/20-file-storage.md): descarga y comprobaciones de archivos/rutas.
- [UD02, autorización](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/17-autorizacion.md): políticas, 401 y 403.
- Fragmento de `FunkosController` aportado por Lucia: organización de acciones y bajas con `200` y booleano. No se copia su actualización de imagen que elimina el archivo anterior.

El enunciado aportado fija RF-09, RF-10, RF-46 y RF-48. Se adaptan los ejemplos a los servicios existentes, sin añadir paquetes ni reproducir la arquitectura de otro proyecto.

## Endpoints y permisos

| Método | Ruta | Acceso | Resultado |
|---|---|---|---|
| GET | `/api/Marca` | Admin | 200, marcas activas |
| GET | `/api/Marca/{id}` | Admin | 200, marca por ObjectId |
| POST | `/api/Marca` | Admin | 201 y Location |
| PUT | `/api/Marca/{id}` | Admin | 200, marca actualizada |
| DELETE | `/api/Marca/{id}` | Admin | 200, booleano; baja lógica |
| GET | `/api/Concesionario` | Admin | 200, concesionarios activos |
| GET | `/api/Concesionario/{id}` | Admin | 200, concesionario por long |
| POST | `/api/Concesionario` | Admin | 201 y Location |
| PUT | `/api/Concesionario/{id}` | Admin | 200, concesionario actualizado |
| DELETE | `/api/Concesionario/{id}` | Admin | 200, booleano; baja lógica |
| GET | `/api/Fotos` | Público | 200, metadatos activos |
| GET | `/api/Fotos/{nombre}` | Público | 200, metadatos |
| GET | `/api/Fotos/{nombre}/archivo` | Público | 200, contenido de la imagen |
| GET | `/api/Fotos/historico/{nombre}` | Trabajador, Responsable, Admin | 200, metadatos incluso de baja |
| GET | `/api/Fotos/historico/{nombre}/archivo` | Trabajador, Responsable, Admin | 200, fichero conservado |
| POST | `/api/Fotos` | Trabajador, Responsable, Admin | 201 y Location |
| DELETE | `/api/Fotos/{nombre}` | Trabajador, Responsable, Admin | 200, booleano; baja lógica |

Se interpretan marcas y concesionarios como catálogos administrativos independientes (RF-46). No se amplían sus permisos de consulta al público; las fotos sí tienen consulta pública explícita en RF-48.

POST y PUT de marcas/concesionarios reciben JSON con los DTOs existentes. El servicio ejecuta FluentValidation y conserva los errores por campo. El PUT de marca sustituye toda la lista `ModelosDisponibles`.

POST de fotos recibe `multipart/form-data` con el campo `archivo`. El nombre generado es la clave primaria propia de Foto en este proyecto. La carpeta queda fijada a `vehiculos`, no la elige el cliente. La descarga usa ese nombre y no una ruta arbitraria. `Location` apunta a los metadatos; el contenido se consulta añadiendo `/archivo`.

## Errores

### Permisos de vehículos (RF-46)

La distribución siguiente es una decisión provisional del proyecto: RF-46 permite
al trabajador gestionar vehículos, pero no enumera cada operación. Se reserva la
incorporación y retirada de flota al administrador; el personal consulta unidades
y modifica sus datos. No es una matriz literal proporcionada por el enunciado.

| Método | Ruta | Público/Cliente | Trabajador | Responsable | Admin |
|---|---|---|---|---|---|
| GET | `/api/Vehiculo` | No | Sí | Sí | Sí |
| GET | `/api/Vehiculo/paged` | No | Sí | Sí | Sí |
| GET | `/api/Vehiculo/{id}` | Sí | Sí | Sí | Sí |
| GET | `/api/Vehiculo/matricula/{matricula}` | Sí | Sí | Sí | Sí |
| GET | `/api/Vehiculo/search` | Sí | Sí | Sí | Sí |
| POST | `/api/Vehiculo` | No | No | No | Sí |
| PUT | `/api/Vehiculo/{id}` | No | Sí | Sí | Sí |
| DELETE | `/api/Vehiculo/{id}` | No | No | No | Sí |
| PATCH | `/api/Vehiculo/{id}/restore` | No | No | No | Sí |

Se utiliza `[Authorize]` como protección general y una política en cada acción
interna. Las tres consultas públicas mantienen `[AllowAnonymous]`. La política
`PolicyNames.Trabajador` ya incluye Trabajador, Responsable y Admin. No se mantiene
una política Admin en la clase porque impediría el acceso del trabajador.

Modelo: [ProductosController de 15-ProductosIdentity](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/15-ProductosIdentity/ProductosTest/Controllers/ProductosController.cs),
con permisos distintos por acción, y UD02 §17.5 para reutilizar políticas existentes.
Se descarta abrir las escrituras al cliente o copiar literalmente los roles ADMIN/USER
del ejemplo, que no son los perfiles de esta práctica.

Los tests unitarios comprueban los atributos y las políticas, no respuestas HTTP
reales. Sigue pendiente demostrar 401 sin sesión y 403 sin permiso contra la API.
En consultas por ID y matrícula, el controller comprueba además la visibilidad:
público y cliente solo reciben unidades con `IsActivo = true` y estado `Disponible`.
Si la unidad no es visible se devuelve 404 con el mismo error de dominio que si
no existiera; es una decisión del proyecto para no exponer datos de la flota interna,
no un código exigido expresamente por RF-46 para este caso.
Trabajador, Responsable y Admin autenticados pueden consultar cualquier estado,
incluidas unidades dadas de baja. La comprobación se hace después del servicio,
también cuando este recupera el DTO desde la caché; no se guardan vistas filtradas
por perfil bajo una clave de caché compartida.

En búsquedas agrupadas se mantiene RF-06: solo Admin ve grupos sin unidades libres;
el resto no recibe esos grupos. El recuento total incluye unidades ocupadas, sin
convertirlas en unidades disponibles. La disponibilidad por fechas sigue pendiente.

Guía para la comprobación dentro de la acción: [UD02 §17.7, autorización basada en recursos](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/17-autorizacion.md#177-autorización-basada-en-recursos).
Se adapta la comprobación de rol y del recurso a la visibilidad de vehículos de
RF-46, sin introducir handlers ni modificar los contratos de servicio.

Por preferencia de Lucia se conserva la validación de matrícula en los validadores
de alta y modificación; no se extrae una clase independiente en esta tarea.

Se reutiliza `DomainErrorExtensions`, sin convertir todos los fallos en 400 ni todas las reglas en 409.

| HTTP | Caso |
|---|---|
| 400 | Validación de datos de marca/concesionario; archivo ausente/vacío; identificador de foto con rutas |
| 401 | Sin identificación válida en una acción protegida |
| 403 | Identificado sin el perfil requerido |
| 404 | Marca o concesionario inexistente/inactivo; foto inexistente, foto inactiva en consulta pública o fichero ausente |
| 409 | Teléfono de concesionario duplicado; otros `ConflictError` devueltos por los servicios |
| 422 | `BusinessRuleError` de almacenamiento: tamaño, extensión, MIME o nombre de archivo rechazados |
| 500 | Fallo interno; excepciones inesperadas atendidas por el middleware global |

Los errores de dominio mantienen `{ code, message, details? }`. Los errores automáticos del binding de `[ApiController]` y los de autenticación/autorización no pasan por `ToHttpResult` y no se garantiza ese mismo cuerpo.

## Conservación del histórico y alcance

### Coherencia de marca y modelo en vehículos

`VehiculoService.Create` y `Update` consultan `IMarcaRepository.GetAll()` y comprueban
la combinación antes de recuperar la foto y persistir el vehículo. Se reutiliza el
contrato existente, sin añadir dependencias ni cambiar los DTO de texto. Para el
catálogo actual se consulta la lista activa directamente; se evita validar contra
una copia en caché que pudiera conservar una marca dada de baja.

- Marca desconocida o dada de baja: `ValidationError`, HTTP 400, campo `Marca`.
- Modelo no incluido en `ModelosDisponibles` de esa marca: HTTP 400, campo `Modelo`.
- Un modelo de otra marca también se rechaza, aunque exista en el catálogo.
- Se exige coincidencia exacta de los nombres. No se normaliza el texto ni se
  acepta una coincidencia parcial: se conserva la escritura del catálogo para
  no generar agrupaciones distintas para la misma marca o modelo.
- Cambiar o dar de baja una marca no reescribe ni elimina vehículos históricos.
  Una modificación posterior del vehículo vuelve a validar la combinación enviada.

El enunciado exige coherencia entre marca, modelo y vehículo; la forma concreta
de comprobar la lista y devolver 400 por campo es una adaptación al modelo del grupo,
no un código fijado expresamente por RF-09 para esta combinación.
Guía: [UD02 §8.3, validación y pertenencia a listas](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/08-dtos-mapeadores-validaciones.md#83-validaciones)
y [ProductoService de 15-ProductosIdentity](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/15-ProductosIdentity/ProductosTest/Services/ProductoService.cs)
para comprobaciones previas a la escritura y devolución de `Result` desde el servicio.

Pruebas: `VehiculoServiceCatalogoTests` cubre alta y modificación con marcas
desconocidas, inactivas, modelos de otra marca, diferencias de escritura y
combinaciones válidas. Comprueba 400, el campo concreto y la ausencia de escrituras
y cambios de caché cuando la combinación no es válida.

- La baja de una marca conserva vehículos y documento.
- La baja de un concesionario con vehículos está permitida, según RF-10 y la decisión confirmada por Lucia: conserva las relaciones y registra la fecha de baja. El catálogo excluye los inactivos.
- El endpoint de baja de foto desactiva solo los metadatos y conserva siempre el fichero físico. Se descarta llamar a `DeleteFileAsync` porque borra el contenido y podría romper RF-48. Por ahora no se implementa una purga al finalizar la retención.
- La subida independiente no asigna automáticamente la foto a un vehículo. Se conserva la relación `FotoFk` existente y no se añade en esta tarea un endpoint de sustitución ni un modelo de múltiples fotos por vehículo.
- No se modifica el `DeleteFileAsync` existente: otros flujos que lo utilicen siguen pudiendo eliminar físicamente archivos y requieren revisión antes de declarar RF-48 cubierto en toda la aplicación.

### Fotos: operaciones fallidas e histórico (RF-48)

Se mantiene la secuencia del [controller de 18-ProductosFileStorage](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/18-ProductosFileStorage/ProductosTest/Controllers/ProductosController.cs):
guardar la subida, construir el DTO y llamar al servicio. A diferencia de la
sustitución de imagen de ese ejemplo, aquí no se borra el fichero anterior: RF-48
exige conservar el histórico. Esta diferencia es una adaptación al enunciado.

Si el alta o modificación devuelve un error, o lanza una excepción, el controller
intenta descartar **solo el nombre de la foto recién subida**. Se conserva el código
original (por ejemplo, 400 por datos inválidos o 409 por matrícula duplicada).
`DescartarFotoAsync` realiza una baja lógica condicional mediante
`SoftDeleteSiNoAsociada`; ningún vehículo puede referenciar esa foto, incluidos los
retirados. Así no se desactiva una foto si el vehículo se confirmó y después falló,
por ejemplo, la actualización de caché. No se borra su fichero físico.

Si falla el descarte se registra el error para revisión. No hay reintentos
automáticos ni una tarea de limpieza: no se presenta este procedimiento como una
transacción atómica entre disco y PostgreSQL. Ante una interrupción del proceso
antes de ejecutar el descarte también puede quedar una subida pendiente.
Si falla la copia de bytes tras crear sus metadatos, almacenamiento intenta
igualmente desactivar esa foto para no ofrecerla como imagen activa.

Las fotos retiradas no aparecen en el listado ni en las consultas públicas.
El personal usa las rutas `/api/Fotos/historico/{nombre}` y
`/api/Fotos/historico/{nombre}/archivo`, sin reactivar la foto. El nombre debe ser
el identificador, no una ruta: se mantienen las comprobaciones de path traversal.
Ejemplo de metadatos históricos:

```json
{
  "nombre": "20261123090000_ab12cd34_coche.jpg",
  "ruta": "/uploads/vehiculos/20261123090000_ab12cd34_coche.jpg",
  "extension": ".jpg",
  "tamano": 1024,
  "activo": false,
  "createdAt": "2026-11-23T09:00:00Z",
  "updatedAt": "2026-11-23T10:00:00Z",
  "deletedAt": "2026-11-23T10:00:00Z"
}
```

Errores de estas rutas: 400 si el nombre no es válido, 404 si faltan metadatos o
fichero, 401 sin identificación y 403 sin el perfil de personal. No se han probado
estos dos últimos por HTTP real. El histórico se consulta por nombre; no añade una
galería ni reconstruye qué foto tenía una reserva. El módulo futuro de reservas
deberá conservar la referencia que necesite si exige esa reconstrucción.
Se retienen todos los bytes, también los de subidas descartadas: se prioriza la
conservación frente a una purga que aún no puede consultar reservas.

Pruebas consultadas como guía: [tema 06, unitarias §2.1 y §2.3](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/02-Pruebas-Unitarias.md),
[integración §3.3](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/03-Pruebas-Integracion.md)
y [tema 07, pruebas y artefactos §2.3–2.4](https://github.com/joseluisgs/DespliegueAplicacionesWeb-07-2025-2026/blob/master/02-Integracion-Continua-CI.md).
Se adaptan al proyecto con NUnit, FluentAssertions, AAA, Verify explícito y sus
fixtures de Testcontainers/PostgreSQL y disco Linux existentes.

## Arranque: migraciones y semillas del catálogo

Referencias consultadas:

- [UD02, 12.11. Migraciones](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/12-entity-framework-core.md#1211-migraciones): comandos de `dotnet ef`, flujo de una migración y `Database.Migrate()` en `Program.cs` para aplicar las pendientes.
- [UD02, 12.12. Seed data y logging](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/12-entity-framework-core.md#1212-seed-data-y-logging): semilla condicionada a que la tabla esté vacía.
- [UD02, 26.2 y 26.3](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/26-organizacion-program.md#262-patrón-de-extension-methods-para-configuración): patrón de extension methods y carpeta `Infrastructure/`; `*Extensions.cs` para el pipeline y `*Config.cs` para el registro en DI.
- [Solución 18, `DatabaseInitializationExtensions`](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/18-ProductosFileStorage/ProductosTest/Infrastructures/DatabaseInitializationExtensions.cs) y [`StorageInitializationExtensions`](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/18-ProductosFileStorage/ProductosTest/Infrastructures/StorageInitializationExtensions.cs): extensión sobre `WebApplication`, parámetro `isDevelopment` y registro con Serilog.

Adaptación al proyecto:

- `InitializeCatalogoAsync(app, isDevelopment)` aplica **siempre** las migraciones
  pendientes con `MigrateAsync` y siembra solo en `Development`. El ejemplo deja el
  esquema en manos de `EnsureDeleted`/`EnsureCreated` en desarrollo y no migra en
  producción; aquí no se borra nada porque los datos de catálogo deben conservarse.
- `InitializeStorage(app, configuration, isDevelopment)` **no vacía** `uploads` como
  el ejemplo de clase: RF-48 exige conservar el histórico. Solo crea carpetas y copia
  los archivos que faltan desde `SeedImages`, sin sobrescribir los existentes.
- El orden es almacenamiento y después catálogo, porque `FotoSeeder` lee el tamaño
  real de los archivos en `uploads/vehiculos`. En el ejemplo el orden es el inverso
  porque su semilla usa `HasData` y no depende de archivos.
- `CatalogoSeeder` encadena fotos → marcas → concesionarios → vehículos; cada seeder
  comprueba su tabla o colección y no duplica datos, como la semilla condicional de
  §12.12.
- Las migraciones del catálogo viven en `Migrations/Catalogo/` y usan el contexto
  `PostgreDbContext`; el contexto de usuarios conserva `Migrations/`.

## Comprobaciones

`dotnet build Alquiler-Vehiculos/Alquiler-Vehiculos.slnx --no-restore`: correcto, 0 advertencias y 0 errores.

Arranque del catálogo: 1.469 pruebas unitarias correctas y 6 pruebas de integración
de `CatalogoSeederTests` con PostgreSQL y MongoDB reales. El fixture
`StorageInitializationExtensionsTests` se ejecutó dentro de su contenedor Linux
(2 casos de `SinUploads` más los 4 casos restantes) y pasó completo. La migración se
aplicó sobre un PostgreSQL temporal y creó `Concesionarios`, `Fotos`, `Vehiculos` y
`__EFMigrationsHistory`. Un arranque completo con PostgreSQL, MongoDB y la API viva
registró en el log 12 fotos, 8 marcas, 5 concesionarios y 12 vehículos.

Actualización del flujo de fotos: 1.469 pruebas unitarias correctas. Integración
seleccionada de `FotoRepositoryTests` y `StorageContainerTests`: 24 correctas, cero
fallos. Las tres pruebas externas de `StorageContainerTests` ejecutan además sus
escenarios internos con disco Linux y PostgreSQL real en contenedores. Se han
comprobado descarte de fotos libres, protección de referencias activas/históricas,
fallos al copiar, conservación de bytes y descarga histórica. Las acciones de los
controllers se invocan directamente: no son E2E HTTP ni demuestran 401/403 reales.
