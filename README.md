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
aprovisionar el esquema del catálogo y configurar un Redis accesible.

La clave JWT se configura mediante User Secrets en desarrollo o `Jwt__Key` en el
entorno; no se guarda en `appsettings.json`. La clave que estuvo versionada debe
rotarse. Retirarla del fichero no la elimina de los commits antiguos.
Si se utilizan las cuentas de inicialización existentes, configura también sus
contraseñas fuera del repositorio: `Seed:AdminPassword`, `Seed:ClientePassword` y
`Seed:TrabajadorPassword` en User Secrets (o con `__` en variables de entorno).
Las contraseñas de desarrollo que estuvieron versionadas también deben rotarse.

### Módulos integrados desde dev

Se conservan autenticación, notificaciones de correo, caché, middleware global y
taller. El contrato de autenticación usa `IAuthService.Login`, como el controlador.
Solo queda una declaración de `AppDbContextUsuarios`, en `Data/Users/`.

El identificador de User Secrets sigue siendo `alquiler-vehiculos-back-local`.
El correo mantiene la configuración SMTP de `dev`; su contraseña se configura
fuera del repositorio mediante `Email:Password` o `Email__Password`.

`compose.yaml` conserva PostgreSQL y MongoDB con sus volúmenes y añade Mailpit.
Mailpit ofrece SMTP en `localhost:1025` y la bandeja en `http://localhost:8025`.
Para usarlo hay que configurar `Email:Host=localhost`, `Email:Port=1025`,
`Email:Security=None` y credenciales vacías en el entorno local; añadir el
contenedor no cambia por sí solo la configuración SMTP de la aplicación.

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
`Microsoft.AspNetCore.OpenApi` utiliza la versión 10.0.15 de `dev` y exige
`Microsoft.OpenApi` 2.13.0 o superior: se fija 2.13.0 para evitar la degradación
que produciría conservar la referencia anterior 2.7.5.

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
Los endpoints independientes de marcas y concesionarios aplican la política Admin de RF-46.

`IMarcaService` está registrado y ofrece alta, listado, consulta por ObjectId,
modificación completa y baja lógica con `Result` y errores de validación por campo.
`ModelosDisponibles` es una lista de nombres dentro del documento de marca:
el alta la recibe y el PUT la sustituye completa; los validadores existentes
exigen al menos un modelo y rechazan nombres vacíos. No equivale al stock de vehículos.
Cambiar esta lista o dar de baja la marca no modifica ni borra los vehículos existentes.
Los vehículos conservan Marca y Modelo como texto; su validación contra este catálogo
en las altas/modificaciones de vehículos no está implementada por este servicio.

Pruebas de servicio de RF-09: `MarcaServiceTests` (CRUD, validación de modelos,
errores y caché). No sustituyen las pruebas de endpoints y permisos pendientes.

## Controladores de catálogo y fotos

Implementados `MarcaController`, `ConcesionarioController` y `FotosController`.
Rutas, permisos, errores, referencias de clase y límites de la conservación de fotos:
[docs/controllers-catalogo.md](docs/controllers-catalogo.md).

Las bajas son lógicas. El nuevo endpoint de fotos conserva el fichero físico;
la subida independiente no lo asigna automáticamente a un vehículo.

Este cambio no convierte documentos antiguos cuyo `_id` sea numérico. Si ya hay
marcas guardadas con ese formato, requieren una migración de datos antes de leerlas
con el nuevo modelo; no se han borrado ni transformado automáticamente.
