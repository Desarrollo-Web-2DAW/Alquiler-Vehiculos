# Tests de catálogo y cobertura de las clases solicitadas

## Referencias consultadas y adaptación

- [Tema 06, pruebas unitarias, §2.1 y §2.3](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/02-Pruebas-Unitarias.md): AAA, independencia, NUnit, FluentAssertions y Moq. Se adaptan los ejemplos a los servicios y DTO reales y se verifican las interacciones con `Verify` y `Times` explícitos.
- [Tema 06, cobertura, §2.4.2](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/02-Pruebas-Unitarias.md): Coverlet y ReportGenerator. `scripts/coverage.ps1` adapta el script del apartado para reunir los resultados de host y Docker de una sola ejecución.
- [Tema 06, integración, §3.1 y §3.3](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/03-Pruebas-Integracion.md): dependencias reales, PostgreSQL y ciclo de vida de Testcontainers. Se reutiliza el contenedor Linux de almacenamiento del proyecto, con carpetas y datos aislados.
- [Tema 07, §2.3 y §2.4](https://github.com/joseluisgs/DespliegueAplicacionesWeb-07-2025-2026/blob/master/02-Integracion-Continua-CI.md): separar niveles de pruebas y conservar informes. No se modifica ningún pipeline ni se suben artefactos.
- [UD02, testing, §28.5–28.9](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/28-testing.md): organización, aserciones e interacciones.
- [Ejemplo de `soluciones/15-ProductosIdentity`, `ProductoRepositoryTests.cs`](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/15-ProductosIdentity/ProductosTest.Test/Services/ProductoRepositoryTests.cs): casos de existencia, ausencia y persistencia. Aquí se mantiene la organización local y se comprueba la persistencia desde contextos nuevos cuando corresponde.

Las pruebas simples de propiedades se incluyen por la petición expresa de cubrir
estas clases al 100 % de líneas; no se presentan como una recomendación general
del profesor de probar todos los getters y setters.

## Qué se prueba

### Controladores

`Unit/Controllers/MarcaControllerTests.cs`, `ConcesionarioControllerTests.cs` y
`FotosControllerTests.cs` comprueban:

- Listados con datos y vacíos, consultas y modificaciones correctas.
- Altas con `201`, DTO de respuesta, acción y parámetros de `CreatedAtAction`.
- Bajas con `200` y conservación del booleano del servicio.
- Errores de dominio con su código HTTP, código de error, mensaje y detalle por campo.
- En fotos, rechazo de la descarga y ausencia de fichero; la baja no llama al borrado físico.
- Delegación al servicio con los argumentos esperados y llamadas prohibidas con `Times.Never`.

`Alquiler-Vehiculos.Storage.Tests/FotosArchivoControllerTests.cs` prueba la descarga
con `FotoService`, `StorageService`, `FotoRepository`, PostgreSQL y ficheros reales
dentro de Docker: contenido, MIME conocido, fallback `application/octet-stream`,
nombre inválido, metadatos ausentes y fichero ausente.

Son pruebas de acciones del controlador e integración de almacenamiento, **no E2E
HTTP**. No demuestran que se ejecuten los filtros de autorización o el model binding
de ASP.NET Core. Las políticas se prueban por separado en `IdentityConfigTests`;
esto tampoco sustituye pruebas HTTP de permisos.

### Servicios e infraestructura

- `FotoServiceTests`: validación de archivo y nombre, filtrado de inactivas,
  propagación de `Result`, errores de baja y conservación de los ficheros físicos.
- `AuthServiceTests`: fallos de creación y asignación de rol de Identity,
  agrupación de errores, compensación del alta sin rol, documentos alternativos
  normalizados y login inválido sin consultar cuentas.
- `AuthServiceIntegrationTests`: se amplía el caso de perfil sin email, persona o
  rol; las demás consultas, migraciones y escenarios de login conservan PostgreSQL real.
- `IdentitySeederTests`: roles existentes/nuevos/fallidos; cuentas existentes,
  altas correctas, errores de creación, errores de rol y compensación; configuración
  incompleta. Los errores controlados se aíslan con managers simulados, sin simular EF.
- `IdentityConfigTests`: configuración con y sin sección JWT, opciones de Identity,
  parámetros de validación del token y roles aceptados/rechazados por las políticas.
- `GlobalExceptionHandlerExtensionsTests`: registro y ejecución del middleware.
- Se reutilizan los tests de integración de almacenamiento e inicialización.
  `AppDbContextUsuarios` y `ParteTallerService` ya estaban cubiertos; no se duplican
  pruebas solo para aumentar su número.

### Clases puras

- `UsuariosErrorTests`: factorías de errores de cliente, trabajador, usuario y
  autenticación, comparando tipo, código, mensaje y errores por campo.
- `DomainExceptionsTests`: constructores con mensaje, causa interna y detalle de
  validación en los dos namespaces existentes.
- `UsuariosModelTests`: configuración de API, DTO público, respuesta de error,
  datos y auditoría de usuarios/personas y límites de edad de 18 y 25 años.

## Ejecución y resultado medido

Desde la raíz:

```powershell
.\scripts\coverage.ps1
```

Ejecución comprobada el 22/11/2026: **1312 tests del proyecto principal correctos,
0 fallos y 0 omitidos**. Sus tres casos de almacenamiento ejecutaron además
**48 casos internos en Docker** (36 de almacenamiento, 6 de inicialización y
6 de descarga), todos correctos. No se suman ambas cifras como si fueran casos
independientes de la misma suite.

El informe combina cuatro XML de la misma ejecución: uno del host y tres de
Docker. Los tres controladores y **todas las clases de la lista solicitada**
alcanzan **100 % de cobertura de líneas**. Esto no significa que el proyecto
completo esté al 100 % ni que se hayan recorrido todas las ramas.

| Clase con decisiones | Líneas | Ramas |
|---|---:|---:|
| `FotosController` | 100 % | 100 % (6/6) |
| `FotoService` | 100 % | 100 % (26/26) |
| `IdentityConfig` | 100 % | 100 % (2/2) |
| `IdentitySeeder` | 100 % | 100 % (40/40) |
| `StorageInitializationExtensions` | 100 % | 100 % (24/24) |
| `Cliente` | 100 % | 100 % (2/2) |
| `ParteTallerService` | 100 % | 100 % (26/26) |
| `StorageService` | 100 % | 97,8 % (45/46) |
| `AuthService` | 100 % | 93,9 % (77/82) |

Ramas restantes, sin alterar producción para forzarlas:

- `StorageService.GetFullPath`: el prefijo comprobado empieza por `/`; el retorno
  anterior para rutas enraizadas impide llegar a la rama que lo retira en los
  sistemas de la ejecución. Se mantiene visible, sin excluirla de cobertura.
- `AuthService`: fallbacks defensivos de JWT para email nulo/roles vacíos y el
  documento vacío tras comprobar documentos duplicados. Los flujos actuales
  validan email/documentos y exigen un rol antes de generar el token. No se
  sustituyen validadores ni se fabrican estados contradictorios solo para pintar
  esas ramas de verde.

ReportGenerator sigue avisando de fuentes generados de Regex/OpenAPI no presentes
en disco. No son fallos de tests ni se han añadido exclusiones para inflar el
resultado. El informe principal conserva su configuración previa.
