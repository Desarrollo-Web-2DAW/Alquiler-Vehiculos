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
| 404 | Marca, concesionario o foto inexistentes/inactivos; fichero no encontrado al descargar |
| 409 | Teléfono de concesionario duplicado; otros `ConflictError` devueltos por los servicios |
| 422 | `BusinessRuleError` de almacenamiento: tamaño, extensión, MIME o nombre de archivo rechazados |
| 500 | Fallo interno; excepciones inesperadas atendidas por el middleware global |

Los errores de dominio mantienen `{ code, message, details? }`. Los errores automáticos del binding de `[ApiController]` y los de autenticación/autorización no pasan por `ToHttpResult` y no se garantiza ese mismo cuerpo.

## Conservación del histórico y alcance

- La baja de una marca conserva vehículos y documento.
- La baja de un concesionario con vehículos está permitida, según RF-10 y la decisión confirmada por Lucia: conserva las relaciones y registra la fecha de baja. El catálogo excluye los inactivos.
- El endpoint de baja de foto desactiva solo los metadatos y conserva siempre el fichero físico. Se descarta llamar a `DeleteFileAsync` porque borra el contenido y podría romper RF-48. Por ahora no se implementa una purga al finalizar la retención.
- La subida independiente no asigna automáticamente la foto a un vehículo. Se conserva la relación `FotoFk` existente y no se añade en esta tarea un endpoint de sustitución ni un modelo de múltiples fotos por vehículo.
- No se modifica el `DeleteFileAsync` existente: otros flujos que lo utilicen siguen pudiendo eliminar físicamente archivos y requieren revisión antes de declarar RF-48 cubierto en toda la aplicación.

## Comprobaciones

`dotnet build Alquiler-Vehiculos/Alquiler-Vehiculos.Back/Alquiler-Vehiculos.Back.csproj --no-restore`: correcto, 0 advertencias y 0 errores.

No se han ejecutado tests ni peticiones HTTP. Esta compilación no acredita el funcionamiento de permisos, la persistencia ni el cumplimiento completo de RF-48; siguen vigentes los requisitos de arranque descritos en el README.
