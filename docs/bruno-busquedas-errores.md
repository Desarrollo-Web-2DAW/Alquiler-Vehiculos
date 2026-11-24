# Bruno: carpetas 11 - BUSQUEDAS y 12 - ERRORES CATALOGOS

Parte del reparto: búsquedas, permisos y errores de catálogos (RF-06, RF-09, RF-10, RF-46).
No se modifican las carpetas 4–8, ni la 9/10 del compañero, ni `scripts/bruno-e2e.ps1`.
Numeración propia para no colisionar: 11 usa `[201]–[275]` y 12 usa `[301]–[357]`.

## Comando ejecutado

```powershell
# Solo las carpetas nuevas
.\scripts\bruno-e2e.ps1 -Carpetas @('11 - BUSQUEDAS','12 - ERRORES CATALOGOS')

# Regresión completa del catálogo con las nuevas carpetas y la matriz de permisos
.\scripts\bruno-e2e.ps1 -Carpetas @('4 - VEHICULOS','5 - MARCAS','6 - CONCESIONARIOS','7 - HISTORICO CATALOGO','11 - BUSQUEDAS','12 - ERRORES CATALOGOS') -MatrizPermisos
```

## Resultado (24/11/2026)

| Recorrido | Peticiones | Tests | Fallos | Informe |
|---|---|---|---|---|
| 11 + 12 | 132 | 132 | 0 | `TestResults/bruno/alquiler-e2e-20261124-113633-075-catalogo.xml` |
| 4–7 + 11 + 12 | 253 | 284 | 0 | `TestResults/bruno/alquiler-e2e-20261124-114405-146-catalogo.xml` |
| 8 × 5 perfiles | 50 | 50 | 0 | `TestResults/bruno/alquiler-e2e-20261124-114405-146-permisos-*.xml` |

No se ha encontrado ningún fallo en servicios ni validadores; no se ha tocado código de producción.

## 11 - BUSQUEDAS

Universo propio por ejecución (marca única `BQ<timestamp>`), todas las búsquedas filtran por ella:

| Modelo | Categoría | Unidades |
|---|---|---|
| Alfa | Hatchback | V1 Diesel/Manual, V2 Diesel/Manual **retirado**, V3 Gasolina/Automática |
| Beta | Suv | V4 Híbrido/Automática 7 plazas, V5 Eléctrico/Automática |
| Gamma | Furgoneta | V6 Diesel/Manual **retirado** (grupo sin disponibilidad) |

Cada test compara la lista completa de grupos `(modelo, categoría, totales, libres)`: un filtro
ignorado cambia la cardinalidad y falla.

- 201–210: Arrange (marca, concesionario, 6 vehículos con foto, 2 bajas).
- 211–216: recuentos sin filtro. Admin ve 3 grupos (incluido Gamma 1/0); anónimo, Cliente,
  Trabajador, Responsable y Mecánico ven 2.
- 217–242: filtros de marca (parcial, sin mayúsculas), modelo, categoría, estado, combustible,
  transmisión, plazas, maletero, autonomía y combinaciones; Admin frente a público.
- 243–251: filtros inválidos → 400 con el campo concreto (valor no admitido, numérico, dos valores,
  mínimos fuera de rango, valor no convertible).
- 252–266: cruces perfil × endpoint que faltaban en la carpeta 4: paginar como Trabajador,
  Responsable y Mecánico; consultar por ID y matrícula una unidad retirada (personal 200,
  Cliente/anónimo 404); matrícula disponible como Cliente; PUT como Responsable y Mecánico.
- 267–275: recuento intacto tras los PUT, limpieza y recuento final (Admin 3/0, 2/0, 1/0; público vacío).

## 12 - ERRORES CATALOGOS

Patrón para cada rechazo: leer (deja la entrada en caché) → rechazo → leer por ID (caché) y en el
listado (base de datos) → sin cambios.

- Marcas (301–326): obligatorios, cuerpo vacío, longitudes 1/101 rechazadas y 100 admitida,
  lista de modelos vacía y modelo en blanco (alta y PUT), GET/PUT/DELETE de ObjectId inexistente,
  PUT y DELETE sobre marca dada de baja sin reactivarla ni cachearla.
- Concesionarios (327–357): obligatorios, longitudes, teléfono mal formado, email vacío,
  PUT con teléfono de otro concesionario (409) sin modificar A ni B, PUT conservando su propio
  teléfono (200, no es duplicado), inexistentes, id no numérico (404 sin 500), PUT/DELETE sobre
  dado de baja.

## Observaciones para revisar (no son fallos de los tests)

1. **Dos formatos de 400.** Los campos vacíos (`""`) los rechaza el binding de ASP.NET
   (`[ApiController]` + `Nullable` → `[Required]` implícito) con `ProblemDetails` y clave `errors`,
   antes de llegar a FluentValidation (`{ code: "VALIDACION", details }`). Los tests aceptan
   ambos y exigen el campo. Si se quiere un único formato, habría que suprimir el `Required`
   implícito o unificar `InvalidModelStateResponseFactory`.
2. **Mecánico puede modificar vehículos (PUT 200)**: tiene rol Trabajador y la política
   `Trabajador` lo admite. La tabla de `docs/controllers-catalogo.md` no lo menciona; confirmar.
3. **Las unidades retiradas cuentan en `unidadesTotales`** (Admin ve Alfa 3/2 con V2 retirado).
   Coincide con el código y la doc ("el total incluye ocupadas"), pero conviene confirmar que
   también deben contar las dadas de baja.
4. **El teléfono de un concesionario dado de baja sigue reservado** (409): decisión vigente
   (`ExistsByTelefono` ignora filtros), test 355.

## Pendientes que necesitan otros módulos

No se pueden cerrar solo con Bruno en esta rama (no hay controladores de reservas ni taller):

- Disponibilidad por fechas y horario de oficina: RF-06, RF-11, RF-19, RF-26.
  Hoy la búsqueda con rango válido devuelve 500 a propósito (`VehiculoService.GetByFilters`, TODO).
- Baja de vehículo con reservas vivas y relación con taller: RF-01, RF-07, RF-08.
- Estados `Reservado`, `EnAlquiler` y `Mantenimiento` en los recuentos: no se pueden crear por API;
  solo existen en la semilla.

## Para el integrador

Para incluir las carpetas en la ejecución por defecto, añadir `"11 - BUSQUEDAS"` y
`"12 - ERRORES CATALOGOS"` a `$Carpetas` en `scripts/bruno-e2e.ps1` y una línea por carpeta en
`docs` de `collection.bru`.
