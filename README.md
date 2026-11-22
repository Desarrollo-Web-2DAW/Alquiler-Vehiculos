# Alquiler-Vehiculos

## Arranque local del back

Requiere .NET 10 y Docker Desktop arrancado. La configuración inicial siguiente usa PowerShell 7:

```powershell
$env:POSTGRES_PASSWORD = Read-Host "Contraseña para las bases locales" -MaskInput
docker compose up -d

$proyecto = "Alquiler-Vehiculos/Alquiler-Vehiculos.Back"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=alquiler;Username=postgres;Password=$env:POSTGRES_PASSWORD" --project $proyecto
dotnet user-secrets set "ConnectionStrings:Catalogo" "Host=localhost;Port=5433;Database=catalogo;Username=postgres;Password=$env:POSTGRES_PASSWORD" --project $proyecto
$mongoPassword = [Uri]::EscapeDataString($env:POSTGRES_PASSWORD)
dotnet user-secrets set "MongoDbSettings:ConnectionString" "mongodb://alquiler:${mongoPassword}@localhost:27018/?authSource=admin" --project $proyecto
$jwtKey = Read-Host "Clave JWT del entorno local" -MaskInput
dotnet user-secrets set "Jwt:Key" $jwtKey --project $proyecto
dotnet run --project $proyecto --launch-profile http
```

La contraseña se configura al crear los volúmenes por primera vez. En posteriores
arranques usa la misma; cambiar la variable no cambia las credenciales ya guardadas.
User Secrets conserva las conexiones fuera del repositorio para tu usuario local.

Si ya se han creado los contenedores y guardado las conexiones y la clave JWT,
para volver a arrancar, desde la raíz del repositorio:

```powershell
docker start alquiler-vehiculos-postgres-1 alquiler-vehiculos-mongodb-1
dotnet run --project Alquiler-Vehiculos/Alquiler-Vehiculos.Back --launch-profile http
```

No repitas la configuración inicial con otra contraseña sobre los volúmenes existentes.

- Consultar la URL de escucha y los mensajes de arranque en la consola.
- No hay interfaz Swagger ni endpoint GraphQL habilitados actualmente. Un 404 en
  `/`, `/swagger` o `/graphql` no significa que el back esté apagado.
- PostgreSQL usa el puerto **5433** y MongoDB el **27018**.
- Usuarios aplica sus migraciones al arrancar sin borrar datos. El contexto de
  catálogo todavía no tiene migraciones ni inicialización de esquema en el arranque.
- Detener dependencias conservando sus datos: `docker compose stop`.

La compilación no garantiza todavía el arranque: el catálogo depende de
`ITransactionManager`, cuya implementación no está en esta copia. También falta
aprovisionar el esquema del catálogo y configurar un Redis accesible. Estas
decisiones están registradas en [la revisión del catálogo](docs/revision-catalogo.md).

La clave JWT se configura mediante User Secrets en desarrollo o `Jwt__Key` en el
entorno; no se guarda en `appsettings.json`. La clave que estuvo versionada debe
rotarse. Retirarla del fichero no la elimina de los commits antiguos.

## Pruebas

```powershell
dotnet test Alquiler-Vehiculos/Alquiler-Vehiculos.slnx
```

Detén el back antes de compilar o ejecutar los tests (Ctrl+C en su terminal o Stop
en el IDE): Windows no puede sustituir su ejecutable mientras está abierto.

Para ejecutar con cobertura desde la raíz del repositorio:

```powershell
dotnet test Alquiler-Vehiculos/Alquiler-Vehiculos.slnx --settings Alquiler-Vehiculos/coverage.runsettings --collect:"XPlat Code Coverage"
```

`coverage.runsettings` está junto a la solución `.slnx`. Excluye del informe el
archivo generado `OpenApiXmlCommentSupport.generated.cs`, no el código del proyecto
ni sus tests. Esta exclusión no corrige vulnerabilidades de paquetes NuGet:
`Microsoft.OpenApi` se referencia explícitamente en la versión 2.7.5 para no
resolver la dependencia vulnerable 2.0.0.

Las pruebas de integración crean y eliminan sus propios contenedores y datos aislados.
No utilizan las bases habituales. Las de almacenamiento también usan PostgreSQL real
y se ejecutan dentro de un contenedor Linux mediante el proyecto principal de tests.

## Marcas: identificadores y baja lógica (RF-09)

`MarcaModel.Id` es un `ObjectId` de MongoDB. El mapper deja `ObjectId.Empty` cuando
se crea una marca sin identificador; el driver asigna uno al insertarla. El DTO de
salida y los parámetros del repositorio lo representan como una cadena hexadecimal
de 24 caracteres. Los identificadores de vehículos y concesionarios no cambian.

`MarcaRepository` consulta solo marcas activas y realiza bajas lógicas, conservando
el documento, sus datos y las fechas de auditoría. No ofrece borrado físico.
Los permisos de RF-46 deberán aplicarse en los endpoints cuando se implementen.

Este cambio no convierte documentos antiguos cuyo `_id` sea numérico. Si ya hay
marcas guardadas con ese formato, requieren una migración de datos antes de leerlas
con el nuevo modelo; no se han borrado ni transformado automáticamente.
