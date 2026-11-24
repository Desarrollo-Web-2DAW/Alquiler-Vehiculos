# Bruno: parte de Lucia (fotos y validaciones)

## Ejecutar sin pisar el trabajo de la compañera

Desde la raíz, con Docker Desktop arrancado:

```powershell
.\scripts\bruno-e2e.ps1 -Carpetas @('9 - FOTOS', '10 - VALIDACIONES VEHICULOS')
```

No se ha cambiado el runner compartido ni las carpetas 4–8, 11 o 12.
El comando por defecto del runner **no incluye** estas nuevas carpetas: hay que
pasarlas con `-Carpetas`. Ejecutar las carpetas completas, no peticiones sueltas.
Se reutiliza el entorno efímero de `compose.e2e.yaml`, sin bases habituales.

## Escenarios

### 9 - FOTOS (39 peticiones)

- Subida independiente con Trabajador, metadatos, Location y descarga pública.
- Archivo ausente y vacío (400 con detalle de `archivo`).
- 5 MiB + 1 byte, extensión no permitida y MIME no permitido (422 y código concreto).
  Se construyen los cuerpos en memoria: no se añaden binarios grandes al repositorio.
  MIME y extensión se aíslan para que el fallo no proceda de otra validación.
- Foto inexistente (404) y nombre con `..` (400 de validación).
- Conjunto de fotos tras rechazos: solo aparece la subida válida.
- Comparación de identidades y fotos de toda la flota: la subida independiente
  no cambia ninguna asignación existente.
- Baja lógica y archivo histórico conservado.
- POST, DELETE y ambos GET históricos con Admin, Trabajador, Responsable y Mecánico;
  denegación de los cuatro endpoints con anónimo (401) y Cliente (403).
- Limpieza de todas las fotos propias y comparación final con el conjunto inicial.

### 10 - VALIDACIONES VEHICULOS

Catálogos, vehículo y foto propios, con IDs capturados del alta:

- Alta con matrícula que contiene vocales.
- PUT con precio cero/negativo, maletero -1/6001, autonomía cero,
  kilometraje negativo y ACRISS de cuatro letras no admitidas.
- Cada rechazo exige 400 y el mensaje del campo concreto; no acepta cualquier 4xx.
- Tras los rechazos: DTO original intacto por GET individual y listado,
  mismo conjunto de fotos activas y archivo original todavía descargable.
- Limpieza mediante bajas lógicas de los datos propios.

Ampliación: `V12b` ejecuta una matriz de 26 entradas inválidas tanto en POST como
en PUT (52 peticiones). Incluye longitudes de matrícula/marca/modelo/ACRISS,
vocales, valores ACRISS no admitidos, color numérico/desconocido, precios,
ambos extremos fuera de plazas y maletero, autonomía, kilometraje, fecha
inválida/futura e identificador de concesionario no positivo. Cada caso exige
`VALIDACION` y únicamente el campo correspondiente. `V16b` comprueba también
los extremos admitidos: plazas 2/9, maletero 0/6000, precio 0.01, autonomía 1 y
kilometraje 0. Se reutiliza la petición mediante `bru.runner.setNextRequest`;
hay que ejecutarla con el runner, no como petición suelta.

### Integración del descarte físico

`VehiculoFotosDescarteTests` añade cuatro casos: alta/modificación y rechazo de
dominio/excepción. Integra `VehiculoController`, `StorageService`, `FotoRepository`
y PostgreSQL reales dentro del runner Testcontainers existente. **Solo el servicio
de vehículos es un mock**, para forzar el fallo; los errores reales de validación
se prueban por HTTP con Bruno. No equivale a probar todos los servicios reales
juntos en integración.

Comprueba registro de la foto nueva inactivo con auditoría, sus bytes exactos
conservados, foto anterior activa con sus bytes originales y vehículo persistido
sin cambios. Lee con un contexto nuevo y verifica explícitamente las llamadas al
mock y la operación que no debe ocurrir. La preparación ahora usa las migraciones
reales del catálogo, no `EnsureCreated`; limpia vehículos, fotos y concesionarios
solo dentro de la base aislada. Contenedores de trabajo con nombres fijos:
`alquiler-storage-postgres` y `alquiler-storage-tests`; no ejecutar este fixture
simultáneamente desde dos terminales. Testcontainers puede crear además su
contenedor auxiliar de limpieza.

```powershell
dotnet test Alquiler-Vehiculos/Alquiler-Vehiculos.Tests/Alquiler-Vehiculos.Tests.csproj --filter 'FullyQualifiedName~StorageContainerTests' --logger 'trx;LogFileName=storage-descarte.trx' --results-directory TestResults/storage-descarte-final
```

AAA: los logins, altas y capturas constituyen Arrange; cada bloque HTTP es Act;
los bloques `tests` contienen Assert. Los escenarios siguen el formato existente.

## Evidencia y límites

- Primera ejecución de fotos (F01–F18): **18 peticiones y 18 tests correctos**,
  cero fallos. Informe `TestResults/bruno/alquiler-e2e-20261124-110528-108-catalogo.xml`.
- Ejecución de validaciones V01–V20: **20 peticiones y 20 tests correctos**,
  cero fallos. Informe `TestResults/bruno/alquiler-e2e-20261124-111020-759-catalogo.xml`.
- Ejecución conjunta con todos los permisos: **59 peticiones y 59 tests correctos**,
  cero fallos (39 de fotos y 20 de validaciones).
  Informe `TestResults/bruno/alquiler-e2e-20261124-111206-122-catalogo.xml`.
  Los contenedores y la red de pruebas se eliminaron al terminar.
- Ampliación de integración: **4 grupos correctos** que ejecutan **57 casos
  internos correctos** (40 de almacenamiento, 6 de inicialización, 7 de descarga
  y los 4 nuevos de descarte). Informe principal:
  `TestResults/storage-descarte-final/storage-descarte.trx`; informes internos
  y cobertura en `Alquiler-Vehiculos/Alquiler-Vehiculos.Tests/bin/Debug/net10.0/TestResults/StorageContainer/`.
  El primer intento falló por comparar la fecha en memoria con la fecha
  persistida; se corrigió capturando el estado de la base antes de actuar.
- Última ejecución de Bruno ampliada: **113 peticiones y 113 tests correctos**,
  cero fallos (39 fotos y 74 validaciones/preparación/limpieza).
  Informe `TestResults/bruno/alquiler-e2e-20261124-112831-573-catalogo.xml`.
  En un primer intento se usó por error `ILAZ` como inválido, pero `Z` está
  admitida por el mapper local; se sustituyó por `ILAJ`, que sí debe rechazarse.
  No se cambió el validador para acomodar el test. Ambos fallos iniciales de
  esta ampliación estaban en los datos/comprobaciones de prueba, no en producción.
  Tras las ejecuciones no quedan contenedores activos (`docker ps`).

No son todas las pruebas de la práctica ni una cobertura exhaustiva de los
validadores. No prueban reservas, taller ni disponibilidad por fechas.
La igualdad de fotos activas en Bruno demuestra que no quedan subidas activas
huérfanas, pero no demuestra por sí sola que se conserven sus bytes: esa
comprobación se realiza en los cuatro casos de integración descritos arriba.
La descarga histórica sí comprueba el archivo de las fotos retiradas con nombre conocido.

No ha sido necesario modificar código de producción para los escenarios ejecutados.
No se han ejecutado unitarias en esta tanda. No se han hecho commits ni publicaciones.

## Material que respalda el procedimiento

- [Tema 06 / 04-Automatizacion-APIs.md, 4.3 y 4.4](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/04-Automatizacion-APIs.md): colecciones, autenticación y comprobación de errores.
- [Tema 07 / 02-Integracion-Continua-CI.md, 2.3 y 2.4](https://github.com/joseluisgs/DespliegueAplicacionesWeb-07-2025-2026/blob/master/02-Integracion-Continua-CI.md): resultados e informes de pruebas.
- [Tema 06 / 03-Pruebas-Integracion.md, 3.1 y 3.3](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/03-Pruebas-Integracion.md): PostgreSQL real, ciclo de vida de Testcontainers y comprobación de persistencia. Adaptación: reutilizar el runner Linux existente para almacenamiento, migraciones y lectura mediante un contexto nuevo.
- [Ejemplo 18-ProductosFileStorage / docker-compose.api.yml](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/18-ProductosFileStorage/docker-compose.api.yml): separación de aplicación y dependencias, reutilizada por el entorno existente.
- [Bruno / Body Data](https://docs.usebruno.com/send-requests/REST/body-data.md) y [JavaScript API Reference](https://docs.usebruno.com/testing/script/javascript-reference.md): multipart y `req.setHeader`/`req.setBody`. Adaptación: cuerpos multipart en memoria para fijar MIME, cero bytes y tamaño exacto sin fixtures grandes.

Los valores y mensajes concretos proceden del contrato local de `FotosController`,
`FotoService`, `StorageService` y `CreateVehiculoValidator`; las comprobaciones de
persistencia posterior son la adaptación a este proyecto, no ejemplos literales del profesor.
