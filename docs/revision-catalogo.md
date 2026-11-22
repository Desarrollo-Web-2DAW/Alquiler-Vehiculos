# Revisión del catálogo

## Alcance y estado inicial

Revisión de `CAMBIOS.md` facilitado por la responsable el 22/11/2026, contra la copia de trabajo (no contra el zip). Se mantiene `feature/Vehiculo/fixErrors` por indicación posterior de la responsable; no se crea una rama desde `develop`.

Compilación inicial: `dotnet build Alquiler-Vehiculos/Alquiler-Vehiculos.slnx`: **7 errores y 4 advertencias**. No había proceso del back ejecutándose. Los 626 tests de la entrega no se consideran verificados.

## Contraste de CAMBIOS.md (antes de corregir)

| Afirmación | Estado real | Acción |
| --- | --- | --- |
| Copia con estructura Back/Tests; origen O/C y requisitos RF | La copia disponible contiene esos proyectos; no permite acreditar autorías ni el resultado del zip. No hay enunciado completo de práctica 4 en las rutas del tema consultadas. | Usar el alcance del prompt; no atribuir autorías ni certificar RF no comprobados. |
| 626 unitarias superadas | La solución no compila. | Ejecutar de nuevo e informar del resultado real. |
| NotFoundVehiculo usa FromObjectId | Confirmado, admite id string sin convertirlo a long. | Conservar. |
| NoDisponibleParaBaja sustituye EnAlquiler por conflicto | Confirmado en la fábrica y el servicio transaccional. | Conservar. |
| Posiciones ACRISS corregidas | Confirmado: índices 1, 2 y 3 para carrocería, transmisión y combustible. | Conservar. |
| FotoFk usa foto.Nombre | Confirmado en los dos mappers; contradice HasKey(Ruta). | Conservar mapper y corregir clave de persistencia. |
| ToVehiculoModel mapea concesionario | Confirmado. | Conservar. |
| Alta Disponible y activa | Confirmado; no lee Estado ni IsActivo. | Conservar. |
| PUT lee Estado con Enum.Parse | Confirmado. | Mantener contrato; comprobar valores definidos antes de mapear. |
| Enum.Parse y Contains se mantienen a propósito | Confirmado, pero el prompt vigente pide corregirlos. | Sustituir Contains por mapa explícito y validar enums. |
| Create: Foto obligatoria, sin Estado, ACRISS con Cascade Stop | Confirmado en producción. | Conservar; ajustar coherencia ACRISS con mapper. |
| Update: Foto obligatoria, Retirado rechazado, Cascade Stop | Confirmado en producción. | Conservar; hay tests antiguos contradictorios. |
| IVehiculoService y servicio tienen GetPaged | Confirmado. | Conservar; filtrar bajas en repositorio. |
| Create guarda caché por id y matrícula | Confirmado; ambos comparten prefijo. | Separar espacios de claves e invalidar al mutar. |
| Update copia Marca, Modelo y ConesionarioFk y aplica estado con CambiarEstadoMapper | Confirmado. | Conservar. |
| Transactions usa fábrica NotFoundVehiculo y solo permite baja Disponible | Confirmado. | Conservar. No acredita comprobación de reservas/partes ajenos al catálogo. |
| SoftDelete del repositorio cambia Activo, DeletedAt y Estado con tracking | Confirmado para vehículo. Foto.SoftDelete aún usa ExecuteUpdateAsync; Foto.Restore ya usa tracking (no ambos como indicaba el prompt). | Corregir solo Foto.SoftDelete. |
| Update tiene condición existente == null o !Activo corregida | La condición está, pero dentro del with hay una segunda declaración pegada que impide compilar. | Quitar bloque duplicado, conservar campos y SetValues. |
| Restore vuelve a Disponible | Confirmado para vehículo. | Conservar. |
| CreateVehiculoDto sin Estado/IsActivo | Confirmado, pero quedan referencias en tests. | Corregir tests obsoletos sin añadir campos al contrato. |
| CreateRequestVehiculoDto modificado, candidato a borrado | No existe en esta copia. Tampoco UpdateRequestVehiculoDto. | No crear ni borrar DTOs para reproducir la entrega. |
| Enum combustible F/U corregido | Confirmado F multicombustible sin aire y U etanol con aire; queda errata MulticomustibleConAire. | Conservar valores y corregir errata y referencias. |
| MapperTests reescrito por letras, guards, FotoFk, fechas y ToDto | Están los casos, pero hay una aserción de carrocería seguida de otra de transmisión incompatible. Fecha inválida espera sustitución silenciosa. | Corregir expectativas concretas y ampliar por las correcciones. |
| MapperTests con TestFixture, Category Unit, alta Disponible/activa y estado en PUT | Confirmado. | Conservar. |
| Typo SegundaLeta corregido; F/U devuelven error combustible | Confirmado. | Conservar. |
| ServiceTests usa veh-1, FotoId, CCMD e ISMD | Confirmado; ISMD se acepta en validador pero no en mapper. | Adaptar prueba a rechazo temprano del validador. |
| ServiceTests cubre paginado, alta, caché, Update y Retirado | Casos presentes; algunas llamadas mock omiten tokens opcionales en expresiones. | Comprobar compilación y ajustar al contrato real. |
| TransactionsTests usa fábrica y prueba estados no disponibles sin borrar | Confirmado. | Conservar y mantener Verify de efectos prohibidos. |
| CreateValidatorTests totalmente reescrito sin Estado/IsActivo | Parcial: sigue accediendo a ambos campos inexistentes, además de casos nuevos. | Retirar casos de campos eliminados; no modificar DTO. |
| CreateValidatorTests: foto obligatoria, tabla ACRISS, Cascade, F/U aceptadas y matrículas | Presentes; aceptar F/U contradice mapper y corrección solicitada. Hay expectativas ACRISS contradictorias. | Ajustar al mapa soportado; no ampliar enums de dominio. |
| UpdateValidatorTests: foto obligatoria, Retirado rechazado, Cascade | Presentes; también queda un caso que acepta Retirado. El DTO vacío no espera Foto. | Corregir expectativas obsoletas. |
| ILAU válido, ILAK inválido y Mantenimiento válido | Presentes; ILAU no es mapeable. | Rechazar ILAU según alcance vigente; conservar los demás. |
| CreateVehiculoDtoTests ya no usa Estado/IsActivo | Contrastar compilación tras corregir producción. | No reintroducir campos eliminados. |
| CreateRequestVehiculoDtoTests modificado y con AAA | No existe. | No crear test de un DTO inexistente. |
| EnumTests F/U actualizados | Parcial: el enum ya tiene F/U correctos, pero el test conservaba EtanolConAire = F. | Corregir expectativa y comprobar tanto F como U, además de la errata de M. |
| Snapshot Entorno-Servidor/_entrega-transacciones-vehiculo eliminado | No existe en el árbol disponible; no acredita el borrado del zip. | No borrar otras carpetas. |
| Cuatro duplicados Unit/Enums eliminados y carpeta vacía | Falso en esta copia: están los cuatro archivos. | Eliminar únicamente esos duplicados, conservando Unit/Models/Vehiculo/Enums. |
| DTOs Request de vehículo pendientes por no usarse | No existen aquí. | Sin acción. |
| OpenApi duplicado pendiente | Confirmado 10.0.2/10.0.1; coverlet.collector también está duplicado. | Mantener una referencia por paquete. |

## Pendiente de decisión (no implementar)

| Problema | Opción recomendada | Alternativa |
| --- | --- | --- |
| PUT permite Estado Reservado/EnAlquiler/Mantenimiento sin RF-07 | Acordar quitar Estado del contrato y operaciones explícitas de taller. | Mantener campo con reglas de transición acordadas. |
| Marca en MongoDB impide FK real desde vehículo | Acordar traslado a PostgreSQL. | Mantener MongoDB y validar referencias en aplicación. |
| Falta Modelo; Marca/Modelo son texto y ModelosDisponibles son strings | Acordar entidad Modelo y relaciones. | Mantener textos con reglas de coherencia consensuadas. |
| Ids ObjectId, long y string | Acordar una política común. | Mantener tipos y documentar su representación. |
| IsDeleted frente a Activo | Unificar criterio tras acuerdo. | Documentar equivalencia y conservar contratos. |
| Matrícula con tilde y Carroceria frente a Categoria | Acordar nombres y compatibilidad antes de renombrar. | Conservar nombres y documentarlos. |
| Fotos 1..1 en código frente a 1..N en diagrama | Confirmar cardinalidad con equipo y enunciado. | Conservar 1..1 si se valida su alcance. |
| CCMR: cuarta letra R sin TipoCombustible equivalente | Confirmar significado y representación con equipo; ahora rechazar con 400. | Acordar otro código soportado, sin reinterpretar R. |
| Sin migraciones del catálogo, tablas no creadas al arrancar | Acordar creación/versionado del esquema. | Aprovisionamiento externo documentado. |
| FotoSeeder, MarcaSeeder, ConcesionarioSeeder y VehiculoSeeder sin ejecutar | Acordar inicialización de datos separada. | Mantener alta manual de datos. |
| Redis registrado/configurado, ausente en compose | Acordar servicio Redis para desarrollo. | Redis externo configurado por entorno. |
| Serilog.Log estático mezclado con ILogger | Acordar convención común. | Mantener mezcla documentada. |
| UseGlobalExceptionHandler comentado | Revisar activación con el equipo. | Gestión por endpoints existente, sin prometer captura global. |
| Diagrama desactualizado | Actualizar tras resolver decisiones. | Anotar discrepancias mientras tanto. |
| IVehiculoTransactionsService depende de ITransactionManager sin implementación | Solicitar implementación del compañero y registrarla. | Autorizar expresamente implementar gestor; no crear servicio nuevo en esta tarea. |

## Material consultado

- [Program de ProductosAvanzados](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/08-ProductosAvanzados/ProductosAvanzados/Program.cs): registros antes de Build y validación por ensamblado.
- [Program de ConfigLogging](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/09-ProductosConfigLogging/ProductosConfigLogging/Program.cs): organización de arranque existente.
- [Repositorio EFCore](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/11-ProductosEFCore/ProductosTest/Repositories/ProductoRepository.cs): actualización/baja de entidad rastreada.
- [Tema 06, unitarias](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/02-Pruebas-Unitarias.md), 2.1, 2.3 y 2.4.2: AAA, NUnit, Moq y resultados.
- [Tema 06, integración](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/03-Pruebas-Integracion.md), 3.1 y 3.3: BD real y ciclo del contenedor.
- [Tema 07, CI](https://github.com/joseluisgs/DespliegueAplicacionesWeb-07-2025-2026/blob/master/02-Integracion-Continua-CI.md), 2.3 y 2.4: ejecución e informes. No se modifican pipelines.
- [Validador de ProductosAvanzados](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/08-ProductosAvanzados/ProductosAvanzados/Validators/CreateProductoValidator.cs): reglas Must y mensajes por campo.
- [Repositorio nativo Mongo](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/12-ProductosMongo/ProductosMongo/Repositories/Native/ProductoNativeRepository.cs): filtros por ObjectId y baja lógica; la sustitución de Parse por TryParse es la corrección solicitada, no un comportamiento ya presente en ese ejemplo.
- [Patrón Result](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/07-excepciones-patron-result.md), 7.6–7.8: errores tipados y traducción a HTTP. Se adaptan a los constructores con Code/Message del proyecto, sin modificar DomainError común.
- [Caché](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/14-cache-redis.md), 14.9 y 14.13: invalidación por operación y claves descriptivas; se conservan los servicios existentes.

Las reglas específicas ACRISS y las correcciones funcionales proceden del prompt vigente; los ejemplos se adaptan sin copiar arquitectura adicional.

## Correcciones realizadas y regresiones

Se conserva la rama original por indicación expresa posterior al prompt. No se han creado controladores, endpoints, servicios, repositorios, validadores, DTOs, migraciones ni seeders. Las altas siguen naciendo Disponible/Activo y se conserva Cascade Stop.

| Punto | Corrección | Commit | Prueba o comprobación |
| --- | --- | --- | --- |
| Fase 0 | Una declaración del contexto y reparación de Update | `a2f7b2a`, `2bda5de` | Build y pruebas de actualización activa/inactiva existentes. |
| Fase 0 | Un Build, registros previos, validadores concretos, AddStorage y retirada de la plantilla meteorológica | `2965f29` | Build; revisión de registros. No se afirma arranque mientras falte ITransactionManager. |
| Fase 0 | JSON válido, una AllowedHosts y clave JWT retirada a User Secrets | `2965f29` | Parseo JSON y comprobación sin mostrar valores sensibles. Rotación de la clave expuesta pendiente del responsable. |
| Fase 0 | Referencias NuGet duplicadas y nombres ACRISS | `9ba6c04`, `3e57295` | Restauración/build sin NU1504; pruebas enum conservadas. |
| Fase 0 | Adaptación de fábricas de errores al DomainError existente | `1f4da5e` | Unitarias de servicios y mapper; no se cambia el contrato de errores común del equipo. |
| Fase 0 | Import EF en concesionario, uso del método de estado en seeder/pruebas y contrato de alta en tests | `621b9b1`, `a81eac6`, `1da4952` | Build con cero errores, sin abrir setters ni llamar seeders desde Program. |
| a | Foto.Nombre PK, Ruta única, baja rastreada (Restore ya lo estaba) | `c8b2d93` | 35 pruebas de FotoRepository/contexto: FK por nombre, ruta duplicada y baja/restauración de la misma instancia. Decisión ratificada por la responsable durante la sesión. |
| b–c | Funciones internal comunes y switches explícitos por letra | `b5abb62` | Cada letra A–Z en las posiciones 2–4, ambos mappers y ambos validadores; casos por tipo concreto y F/U conservados. |
| d | Color/Estado exigen valor definido y rechazan texto numérico | `1a9437f` | Casos numéricos definidos/no definidos, negativos y nombres desconocidos; se conservan nombres válidos sin distinguir mayúsculas. |
| e | Fecha inválida/futura rechazada, sin sustituir por UtcNow | `53e588c` | Ambos mappers/validadores y servicios con fecha inválida/futura: error de validación → 400 y guardar nunca llamado. |
| f | Únicamente 23505 del índice de matrícula se traduce a ConflictError | `3b4337e` | Inserción entre comprobación y alta en contextos distintos; matrícula reservada tras baja; FK ajena no se confunde con duplicado; servicio devuelve 409 sin cachear. |
| g | Concesionario existente antes de Create/Update | `2b7a719` | Dos casos con Moq: inexistente → 400, sin foto, guardado ni transacción. |
| h | ObjectId.TryParse en GetById/SoftDelete/ExistById de Marca | `aac8160` | IDs mal formados y vacío devuelven null/false y no alteran documentos; Update queda protegido al llamar GetById. 26 pruebas Mongo pasaron. |
| i | Prefijos vehiculo:id:/vehiculo:matricula: e invalidación al crear/actualizar/borrar/restaurar | `a022215` | Verificaciones explícitas de claves y prueba con id igual a matrícula; invalidación en cascada de concesionario. 55 unitarias de ambos servicios pasaron. |
| j | Solo activos en GetAll/GetPaged y en el total | `e82fa50` | Colecciones con baja y con/sin activo; el histórico se conserva físicamente. |
| k | Comentarios correctos y sin IgnoreQueryFilters innecesario en vehículo | `c8dc15d` | ExistById/ExistByMatricula incluyen histórico; crear con matrícula de baja sigue en conflicto. 25 pruebas de VehiculoRepository pasaron. |
| l | Program/appsettings | Véase Fase 0 | No se crean rutas ni se activan migraciones/seeders de catálogo. |
| m | README sin restos, logs ignorados, revisión del índice | `1c21918` | JSON válido; TestResults, .idea, logs, bin y obj ignorados. Cero archivos de esas carpetas estaban versionados, por lo que no fue necesario git rm --cached. |
| Fallo adicional del catálogo | Update de concesionario ignoraba Nombre/Ciudad/Telefono/Email | `bfab017` | Prueba de integración existente antes roja y ahora verde; conserva Id y CreatedAt. |
| Datos de integración | Teléfonos duplicados en preparación incumplían índice único | `574509b` | Fixtures con teléfonos distintos, sin quitar ni debilitar el índice. 35 pruebas de ambos repositorios pasaron. |

### Cambios mínimos de compatibilidad

`IVehiculoRepository.Create` devuelve ahora `Result<VehiculoModel, DomainError>`. Como el CRUD común devuelve una entidad sin error tipado, la interfaz de vehículo declara sus operaciones existentes sin heredar ese CRUD. Se conserva el resto de firmas y no se modifica `ICrud`, los otros repositorios ni los contratos HTTP. El servicio propaga el conflicto de dominio; las demás excepciones de base de datos siguen siendo excepciones.

La clave JWT antigua se retiró del árbol actual y se guardó fuera del repositorio. **Esto no borra el secreto del historial ni lo rota**. No se reescribe historial ni se publica nada.
La revisión final detectó además la conexión de usuarios y contraseñas de cuentas de inicialización en `appsettings.Development.json`. Se trasladaron a User Secrets junto con las conexiones de MongoDB/Redis, manteniendo en el fichero las opciones no sensibles. Es un cambio mínimo fuera del catálogo para cumplir la ausencia de secretos; no modifica el seeder ni crea cuentas nuevas. También deben rotarse las credenciales que ya estuvieron expuestas en el historial.

## Comprobaciones

- Build inicial: 7 errores / 4 advertencias (los errores de sintaxis ocultaban otros posteriores).
- Al cerrar Fase 0: 0 errores / 0 advertencias. Primera suite: **895 total · 875 OK · 19 KO · 1 omitida**.
- Docker no estaba disponible en la primera comprobación; después se comprobó operativo (`docker info`: 29.9.2) y se ejecutaron las regresiones de integración con PostgreSQL/MongoDB reales en Testcontainers.
- Build final: **0 errores / 0 advertencias**.
- `dotnet test Alquiler-Vehiculos/Alquiler-Vehiculos.slnx --logger "trx;LogFileName=catalogo-final.trx"`: **Total: 926 · OK: 926 · KO: 0 · Omitidas: 0**, repetido tras retirar las credenciales de desarrollo, duración final de tests 37 s. Informe local ignorado por Git: `Alquiler-Vehiculos/Alquiler-Vehiculos.Tests/TestResults/catalogo-final.trx`.
- Sin KO finales. La omisión temporal de Redis en la primera ejecución no se cuenta como éxito; en la ejecución completa final no hubo omitidas.
- No se ejecutaron E2E ni se crearon endpoints para hacerlos posibles. No se arrancó la API ni se escribieron datos en las bases habituales.

## Integración posterior de origin/dev

La responsable autorizó incorporar `origin/dev` (`d6c2027`) en `feature/Vehiculo/fixErrors` y aceptó las resoluciones propuestas. Se mantienen las correcciones del catálogo. Los componentes de otros módulos descritos aquí proceden de `dev`, no se han reimplementado en esta revisión.

| Archivo o colisión | Resolución aplicada |
| --- | --- |
| Back.csproj | UserSecretsId local conservado; MailKit, Tokens y herramientas EF incorporados; plantillas de correo copiadas a salida. Se mantiene FluentValidation 12 y su registro por ensamblado; no se añade FluentValidation.AspNetCore 11 ni Moq a producción, porque el código incorporado no los usa. |
| Versiones NuGet | OpenApi 10.0.15 de dev requiere Microsoft.OpenApi >= 2.13.0: se fija 2.13.0 tras comprobar NU1605. EF Core.Relational se fija a 10.0.8, igual que EF Core/Design, para eliminar MSB3277 entre Back y los proyectos de pruebas. Se conservan las demás versiones locales que funcionan, sin actualizaciones masivas. |
| DomainError.cs | Se mantienen los errores tipados existentes y se incorpora ApiErrorResponse de dev. |
| Program.cs | Se combinan los registros del catálogo/usuarios/storage/caché con AddEmailNotifications y AddTallerServices antes de un único Build. Se activa el middleware global incorporado desde dev. No se añaden migraciones ni llamadas a seeders del catálogo. |
| AuthService/IAuthService | Se conserva Login de dev, coherente con el controlador incorporado. El cuerpo del servicio se mantiene. |
| appsettings*.json | Se combinan opciones de correo con catálogo/usuarios, sin reintroducir clave JWT ni contraseñas de conexiones o cuentas iniciales. Se conserva el UserSecretsId local; Email:Password debe configurarse en ese almacén o en variables de entorno. |
| Tests.csproj | Se conservan PostgreSQL, MongoDB, cobertura y proyecto auxiliar Storage; se incorpora Testcontainers.Redis. No se cambian tests para ocultar fallos. |
| compose.yaml | Se mantienen PostgreSQL/MongoDB, puertos y volúmenes; se añade Mailpit de dev. No se arrancan ni recrean las bases habituales. Redis sigue pendiente, como antes. |
| AppDbContextUsuarios duplicado | Ambas declaraciones tienen las mismas tablas, índices y propiedades; difieren en comentarios. Se conserva Data/Users/AppDbContextUsuarios.cs y se elimina la copia incorporada en Entities, sin perder configuración. |

La activación del middleware global deja resuelto ese pendiente de integración; el resto de decisiones del equipo, en particular la implementación ausente de ITransactionManager, no se implementa por este merge.

### Comprobaciones de la integración

- Se reutilizaron los apartados de pruebas de los temas 06 y 07 leídos en esta sesión y se volvió a consultar el Program de ConfigLogging enlazado arriba como guía de orden de registros/pipeline.
- `dotnet build Alquiler-Vehiculos/Alquiler-Vehiculos.slnx`: **0 errores, 0 advertencias**.
- `dotnet test Alquiler-Vehiculos/Alquiler-Vehiculos.slnx --logger "trx;LogFileName=integracion-dev.trx"`: **Total: 1037 · OK: 1037 · KO: 0 · Omitidas: 0**, duración de tests 52 s. Incluye las pruebas incorporadas desde dev.
- Docker disponible (29.9.2); las pruebas SMTP utilizan Mailpit aislado mediante Testcontainers, no el servidor de correo externo configurado para la aplicación.
- Sin marcadores de conflicto en los archivos de trabajo, sin problemas en `git diff --check` y sin secretos detectados por la revisión de configuración. `docker compose config --quiet` válido con una variable temporal de validación, sin levantar servicios ni modificar volúmenes.
- No se ha arrancado la API, realizado E2E ni enviado correo externo. La compilación y las pruebas no certifican el arranque mientras falte ITransactionManager.
