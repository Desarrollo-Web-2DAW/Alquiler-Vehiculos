# Bruno: continuación del catálogo

## Ejecución aislada

Desde la raíz, con Docker Desktop arrancado:

```powershell
.\scripts\bruno-e2e.ps1 -MatrizPermisos
```

El script utiliza Bruno CLI **4.3.0** en un contenedor. Antes del recorrido imprime
su versión y su ayuda, construye la API real y espera un `/health` correcto.
Ejecuta las carpetas 4–7 en orden. Con `-MatrizPermisos`, repite además los diez
endpoints de la carpeta 8 para anónimo, Cliente, Trabajador, Responsable y Mecánico,
guardando un informe separado por perfil. Sin esa opción no ejecuta la matriz.

Para ejecutar solo el histórico:

```powershell
.\scripts\bruno-e2e.ps1 -Carpetas @('7 - HISTORICO CATALOGO')
```

`compose.e2e.yaml` es independiente del compose de desarrollo. Usa PostgreSQL
con las dos bases (`alquiler` y `catalogo`), MongoDB, Redis y Mailpit reales.
No publica puertos ni usa las bases, las subidas o los contenedores habituales.
Los nombres son cortos y fijos: `alquiler-e2e-api`, `alquiler-e2e-postgres`,
`alquiler-e2e-mongo`, `alquiler-e2e-redis`, `alquiler-e2e-mail` y `alquiler-e2e-bruno`.
No admite dos ejecuciones simultáneas: si ya existe el entorno, el script se detiene
sin modificarlo. Solo los nombres de los informes incluyen fecha y hora.
Las contraseñas y la clave JWT se generan por ejecución y no se escriben en archivos.

Las bases y las subidas son efímeras. El `finally` del script elimina solo los
contenedores y la red de **esa ejecución**, incluso al fallar. No ejecuta limpiezas
globales de Docker. Los informes JUnit quedan en `TestResults/bruno/` (ignorado por Git).
Se omiten cabeceras y cuerpos en el informe para no guardar credenciales ni tokens.
El script rechaza un informe sin tests o con fallos, además de comprobar la salida de Bruno.
Si se cierra forzosamente la terminal y no se llega a ejecutar `finally`, revisar
en Docker Desktop los contenedores con el prefijo `alquiler-e2e`.

No se usa `--parallel`: cada carpeta es un escenario con pasos dependientes.
Se usa `--bail` para no encadenar peticiones con un ID sin crear.

## Qué comprueban los nuevos escenarios

| Peticiones | Requisitos o contrato comprobado |
| --- | --- |
| 091, 097–099, 101–104 | RF-09: alta, consulta, listado, modificación y baja de marcas; sustituir modelos y leer después para detectar caché obsoleta. |
| 092–096 | RF-46: 401 anónimo y 403 para Cliente, Responsable, Trabajador y Mecánico en operaciones representativas. |
| 100, 105 | Contrato existente: error en el modelo vacío e identificador no interpretable sin 500. |
| 106, 112, 114–115, 117–120 | RF-10: CRUD de concesionarios, listado y lectura posterior al PUT/DELETE para detectar caché obsoleta. |
| 107–111 | RF-46: 401 y 403 en operaciones representativas de concesionarios. |
| 113 | Decisión existente del proyecto: teléfono único (409), no requisito adicional inventado de RF-10. |
| 116 | RF-19: formato de correo inválido y detalle del campo `Email`. |
| 121–141 y 152 | RF-01, RF-09, RF-10 y RF-48: vehículo con catálogos propios; cambiar foto; bajas de marca y concesionario sin cascada; baja de vehículo; retirada de foto; archivo histórico accesible al personal y denegado al público/Cliente. El 152 consulta el listado sin caché individual. |
| 142–151 por perfil | RF-46: los diez endpoints CRUD de marcas y concesionarios con cada perfil no administrador. Admin se cubre con el CRUD de las carpetas 5 y 6. |

Se mantienen nombres, secuencias y formato `.bru` de la colección anterior.
AAA: la autenticación y los datos propios son **Arrange**, el bloque HTTP es
**Act**, y las comprobaciones del bloque `tests` son **Assert**.
Los IDs se capturan del alta; no se borran ni modifican marcas o concesionarios semilla.
La última baja limpia la vista activa, pero conserva el registro histórico.

Las comprobaciones existentes de vehículos 062, 066, 068, 078 y 079 se refuerzan:
ID de 11 caracteres, estado inicial `Disponible`, baja `Retirado` junto con
`isActivo=false`, restauración `Disponible` y errores concretos de `Marca`/`Modelo`.

## Límites: no equivalen a todos los requisitos superados

### Evidencia del 24/11/2026

Última ejecución: `scripts/bruno-e2e.ps1 -MatrizPermisos`.
**171 peticiones y 202 tests correctos, 0 fallos**: 121 peticiones/152 tests
del recorrido de catálogo e histórico y 50 peticiones/tests de permisos.
Informes: `TestResults/bruno/alquiler-e2e-20261124-104615-110-*.xml`.
Incluye la consulta sin caché individual (152) tras las bajas de los catálogos.
No ha requerido cambios nuevos en la lógica de producción.

Evidencia anterior, antes de incorporar histórico y matriz:

Ejecución de `4 - VEHICULOS`, `5 - MARCAS` y `6 - CONCESIONARIOS` con la API
arrancada y PostgreSQL, MongoDB y Redis reales:

- **99 peticiones correctas y 130/130 tests correctos**.
- Informe local: `TestResults/bruno/alquiler-vehiculos-e2e-20261124-102129-761.xml`.
- La API se compiló/publicó dentro de Docker y alcanzó `/health` antes del recorrido.
- Contenedores y red de pruebas eliminados al finalizar; Funko no se modificó.
- No ha sido necesario cambiar lógica de producción para estos escenarios.

Se movieron las comprobaciones de las carpetas 4–6 a `tests`: en la primera
ejecución los callbacks de `post-response` aparecían en consola y JUnit, pero el
resumen de CLI mostraba 0 tests. Con `tests`, resumen e informe registran los casos.

### Pendiente

- La matriz de la carpeta 8 se limita a marcas y concesionarios; no equivale a la matriz
  completa de vehículos, fotos ni de todos los módulos del proyecto.
- Un 404 tras DELETE no demuestra la permanencia física del registro: también debe
  comprobarse en integración con la base real.
- El histórico se comprueba con un vehículo sin reservas; no sustituye un flujo con
  reservas reales ni la comprobación de sus relaciones históricas.
- No resuelven disponibilidad por fechas ni comprobación de reservas vivas al dar
  de baja: siguen pendientes los contratos con reservas.
- Los resultados reales de cada ejecución están en su informe; crear una petición
  no demuestra que haya pasado.

## Material consultado

- [Tema 06, automatización de APIs, 4.3 y 4.4](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/04-Automatizacion-APIs.md): colecciones, autenticación, casos negativos y ejecución.
- [Tema 07, CI, 2.3 y 2.4](https://github.com/joseluisgs/DespliegueAplicacionesWeb-07-2025-2026/blob/master/02-Integracion-Continua-CI.md): pruebas e informes como evidencia.
- [Ejemplo del profesor: compose de API con almacenamiento](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/18-ProductosFileStorage/docker-compose.api.yml) y su [Dockerfile](https://github.com/joseluisgs/DesarrolloWebEntornosServidor-02-2026-2027/blob/main/soluciones/18-ProductosFileStorage/Dockerfile): aplicación y dependencias separadas. Adaptación: aislamiento efímero de E2E, MongoDB para marcas y Bruno CLI.
- [Bruno CLI: ejecución](https://docs.usebruno.com/bru-cli/run/overview): carpetas, variables y sandbox `developer` para los scripts existentes con `axios`.
