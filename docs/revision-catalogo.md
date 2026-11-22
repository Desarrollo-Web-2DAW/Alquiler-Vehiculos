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
| EnumTests F/U actualizados | Confirmado en Unit/Models/Vehiculo/Enums. | Conservar y actualizar errata de M. |
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

Las reglas específicas ACRISS y las correcciones funcionales proceden del prompt vigente; los ejemplos se adaptan sin copiar arquitectura adicional.

## Comprobaciones

Pendientes las comprobaciones posteriores a las correcciones. Este documento no certifica todavía una compilación ni una suite verde.
