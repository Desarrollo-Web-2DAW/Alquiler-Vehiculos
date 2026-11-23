# Pruebas de búsqueda y consulta de vehículos

## Alcance

Ampliación centrada en vehículos, sin implementar contratos del módulo de reservas.
Se han añadido **206 casos ejecutables** con NUnit y FluentAssertions, patrón AAA,
validadores reales y Moq solo para aislar dependencias de servicios y controladores.
Las interacciones comprobadas llevan `Verify` y los efectos prohibidos `Times.Never`.

| Archivo de pruebas | Casos nuevos | Comportamientos |
| --- | ---: | --- |
| `Unit/Validators/Catalogo Vehiculos/Query/QueryRequestVehiculoValidatorTests.cs` | 67 | Clasificaciones válidas, minúsculas, campos opcionales, valores inválidos y numéricos, combinaciones no permitidas, mínimos y fechas: pareja, pasado, mismo día, orden y límites 1/30/31 días. |
| `Unit/Services/VehiculoServiceQueryTests.cs` | 48 | Consulta por matrícula en caché, repositorio y ausencia; claves por ID/matrícula; validación por campo; agrupación, totales y libres; vista pública/administrador; dependencia de disponibilidad pendiente y un caso adicional de modificación inválida. |
| `Unit/Dto/Catalogo Vehiculos/Query/QueryVehiculoDtoTests.cs` | 4 | Valores iniciales de DTOs, respuesta y error específico de matrícula con conversión a 404. |
| `Integration/Repositories/Vehiculo/VehiculoRepositoryQueryTests.cs` | 46 | Filtros individuales y combinados sobre PostgreSQL real; coincidencias parciales e insensibles a mayúsculas; `%` y `_` como texto literal; límites numéricos; estados; histórico; orden, lectura sin tracking y sin cargar fotos; entradas directas no admitidas. |
| `Unit/Controllers/VehiculoControllerTests.cs` | 41 | Acciones CRUD existentes, paginación y metadatos, DTOs multipart completos, errores de almacenamiento, códigos de dominio y ubicación del alta. |

Los archivos parciales de servicio y repositorio reutilizan sus fixtures existentes,
incluida la preparación y limpieza de datos. Los nombres originales se conservan.
El repositorio utiliza `RepositoryTestBase`: Testcontainers, PostgreSQL 17 Alpine,
puerto dinámico, base aislada, limpieza por caso y eliminación del contenedor.

## Error real detectado y corregido

Los casos con `Sedan, Suv`, `Disponible, Reservado`, `Gasolina, Diesel` y
`Manual, Automatica` demostraron que la conversión de enum aceptaba combinaciones
que podían terminar representando un valor definido. Cada filtro es una opción
única, no una selección múltiple. Ocho casos fallaron antes de corregirlo.

Se rechaza la coma tanto en `QueryRequestVehiculoValidator` como en la protección
de `VehiculoRepository.GetByFilters`. El servicio devuelve 400 con el campo;
una llamada directa no validada al repositorio conserva el contrato de resultado vacío.
No se han relajado las aserciones ni convertido errores de dominio en excepciones.

## Ejecución y cobertura

Ejecución completa del proyecto principal de pruebas el 23/11/2026:

```powershell
dotnet test Alquiler-Vehiculos/Alquiler-Vehiculos.Tests/Alquiler-Vehiculos.Tests.csproj --no-restore --settings Alquiler-Vehiculos/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory Alquiler-Vehiculos/Alquiler-Vehiculos.Tests/TestResults/query-final-20261123145737
```

**Total: 1519 · OK: 1519 · KO: 0 · Omitidos: 0.** Duración comunicada: 1 min 5 s.
Docker se comprobó previamente con `docker info` (servidor 29.9.2).

Informe generado:

`Alquiler-Vehiculos/Alquiler-Vehiculos.Tests/TestResults/query-final-20261123145737/230f4a40-827f-4fe3-99df-d9377db6290c/coverage.cobertura.xml`

El informe muestra **100 % de líneas y ramas** en:

- `QueryRequestVehiculoValidator`.
- `VehiculoService.GetByMatricula` y `GetByFilters`, incluidas las proyecciones de grupos.
- `VehiculoRepository.GetByFilters`.
- Los dos DTOs de query.
- Las acciones existentes de `VehiculoController`.

Esto **no es un 100 % global del proyecto ni demuestra requisitos aún no implementados**.
Persisten huecos defensivos en el CRUD anterior: fallo del mapper tras una validación
que ya rechaza las mismas entradas, y guardas internas de ACRISS precedidas de
`CascadeMode.Stop`. No se han forzado mediante reflexión ni alterado las reglas para
alcanzar esas ramas. La cobertura no sustituye los escenarios de negocio pendientes.

## Pendiente y límites

- **RF-26:** disponibilidad real por rango, horario de oficina, tarifa mínima y precio
  estimado. La prueba actual documenta el error provisional de integración, no el éxito
  de una búsqueda por fechas.
- **RF-46:** `esAdmin` prueba la visibilidad en el servicio, pero no el origen seguro del
  rol. Debe obtenerse del usuario autenticado en el controlador. Las llamadas directas
  a acciones no ejecutan el middleware: no demuestran 401/403.
- No hay todavía acciones de consulta por matrícula y búsqueda agrupada conectadas
  en `VehiculoController`, ni colección Bruno en esta copia. **No se han realizado E2E**
  de estos métodos ni se presenta la integración del repositorio como E2E.
- Las pruebas no implementan ni garantizan las integraciones pendientes de reservas,
  taller, caché de disponibilidad o la concurrencia de reservas.

## Material consultado

- [Tema 06 / 02-Pruebas-Unitarias.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/02-Pruebas-Unitarias.md), apartados 2.1, 2.3 y 2.4.2: AAA, NUnit, Moq y cobertura.
- [Tema 06 / 03-Pruebas-Integracion.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/03-Pruebas-Integracion.md), apartados 3.1 y 3.3: motor real, contenedores y su ciclo de vida.
- [Tema 07 / 02-Integracion-Continua-CI.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-07-2025-2026/blob/master/02-Integracion-Continua-CI.md), apartados 2.3 y 2.4: ejecución e informes como evidencia. No se ha cambiado ningún pipeline.

Adaptación: se conserva el stack y la organización del proyecto, incluyendo Result,
los fixtures existentes y PostgreSQL 17, sin añadir paquetes ni copiar una arquitectura mayor.
