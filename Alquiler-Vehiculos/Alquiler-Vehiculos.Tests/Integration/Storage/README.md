# Pruebas de almacenamiento con Testcontainers

## Organización

- `Unit/Validators/.../UpdateVehiculoValidatorTests.cs`: reglas actuales del DTO de actualización, con objetos reales, NUnit, FluentAssertions y AAA.
- `Integration/Storage/StorageContainerTests.cs`: inicia un contenedor Linux .NET 10 con Testcontainers, ejecuta dos grupos NUnit y comprueba que se han ejecutado casos y que todos pasan.
- `Alquiler-Vehiculos.Storage.Tests`: proyecto auxiliar con los tests que acceden al sistema de archivos **dentro del contenedor**. No se añade a la solución para evitar ejecutarlo directamente en el PC. La referencia desde el proyecto principal lo compila y copia sus binarios automáticamente.

No se montan `uploads`, `SeedImages` ni otras carpetas del PC. Cada caso crea su raíz aislada en el contenedor y la elimina al terminar; `DisposeAsync` elimina el contenedor. El contenedor usa un usuario sin privilegios para probar realmente el fallo de borrado por permisos, no un logger que lance una excepción artificial.

## Ejecución

Desde `Alquiler-Vehiculos.Tests`, con Docker Desktop arrancado en modo Linux:

```powershell
docker info
dotnet test --filter "TestCategory=Unit"
dotnet test --filter "TestCategory=Integration"
dotnet test --collect:"XPlat Code Coverage"
```

La primera ejecución necesita acceso para descargar la imagen `mcr.microsoft.com/dotnet/sdk:10.0` y la imagen del Resource Reaper de Testcontainers. No se publica código ni se crea una imagen propia.

## Cobertura y resultados

Hay procesos separados: el colector del host no mide el código ejecutado dentro de Docker.

- Cobertura del host: `TestResults/<guid>/coverage.cobertura.xml`.
- TRX y cobertura recuperados de Docker: `bin/Debug/net10.0/TestResults/StorageContainer/<guid>/<clase>/` (para Release, cambia Debug por Release).
- Los informes de Docker se adjuntan también al resultado NUnit.
- Se comprueba que la cobertura de Docker no esté vacía. `storage.runsettings` permite instrumentar los DLL/PDB trasladados desde Windows aunque los fuentes no estén en Linux; no excluye líneas ni ramas para elevar porcentajes.

Para generar un informe conjunto con ReportGenerator, selecciona los tres XML de **la misma ejecución** (host, StorageService e InitializeStorage):

```powershell
reportgenerator "-reports:<xml-host>;<xml-storage>;<xml-inicializacion>" "-targetdir:coveragereport" -reporttypes:Html
```

No uses solo el XML del host para evaluar la cobertura de almacenamiento ni mezcles informes antiguos de otras versiones.

La rama que retira `/uploads/` en `GetFullPath` sigue siendo inalcanzable: `Path.IsPathRooted` devuelve true antes. Los tests documentan este comportamiento; no se modifica producción para forzar cobertura. `SaveFileAsync` sigue sin esperar `CopyToAsync`: las pruebas con contenido pequeño no garantizan el comportamiento de copias asíncronas pendientes.

## Referencias consultadas

- [Tema 06, 02-Pruebas-Unitarias.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/02-Pruebas-Unitarias.md): 2.1, 2.3 y 2.4.2 (AAA, NUnit/Moq y cobertura).
- [Tema 06, 03-Pruebas-Integracion.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/03-Pruebas-Integracion.md): 3.1 y 3.3 (contenedores, ciclo de vida y aislamiento).
- [Tema 07, 02-Integracion-Continua-CI.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-07-2025-2026/blob/master/02-Integracion-Continua-CI.md): 2.3 y 2.4 (ejecución y artefactos; no se ha añadido pipeline).
- [Ejemplo de almacenamiento del profesor](https://github.com/joseluisgs/TiendaDawApi-NetCore/blob/main/TiendaApi.Tests/Integration/Services/Storage/FileSystemStorageServiceIntegrationTests.cs): escenarios de guardado, borrado, rutas y validaciones. Adaptación: ejecutarlos dentro de Docker, con comprobación del contenido persistido y errores concretos de Result.
- [Testcontainers: creación y copia/lectura de archivos](https://dotnet.testcontainers.org/api/create_docker_container/) y [Resource Reaper](https://dotnet.testcontainers.org/api/resource_reaper/).
- [VSTest](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-vstest) y [Coverlet/VSTest](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/VSTestIntegration.md): ejecución de DLL, colector y configuración de cobertura sin fuentes locales.
