---
name: tests-daw
description: >-
  Crear, ampliar, revisar y corregir tests de proyectos .NET/C# de DAW.
  Usar NUnit y FluentAssertions en unitarias, Moq solo cuando haga falta
  con Verify explícito, patrón AAA siempre, integración con Testcontainers
  para .NET y Docker, y E2E de API con Bruno. Consultar obligatoriamente
  los repositorios 06 y 07 de DespliegueAplicacionesWeb de joseluisgs.
  Aplicar al pedir tests, pruebas, cobertura, mocks, integración o E2E.
---

# Tests DAW

Trabajar en castellano y escribir pruebas claras para una alumna de 2.º de DAW.
Adaptar nombres, arquitectura y versiones al proyecto real.

## Reglas fijas de la usuaria

| Tipo de prueba | Herramientas y alcance |
| --- | --- |
| Unitaria | NUnit + FluentAssertions. Usar Moq únicamente para dependencias que necesiten un doble. |
| Integración | NUnit + FluentAssertions + Testcontainers para .NET, ejecutando servicios reales en Docker. |
| E2E de API | Bruno, con peticiones HTTP a la aplicación arrancada y sus dependencias reales. |
| Estructura | Aplicar Arrange, Act y Assert en todas las pruebas. |
| Mocks | Añadir Verify explícito en cada test que utilice Moq, con argumentos y Times adecuados. |
| Fuentes | Leer el material relevante del profesor antes de implementar las pruebas. |

No sustituir estas herramientas por otras por preferencia personal.
No añadir Moq a una clase pura que se pueda probar con objetos reales.
Aplicar las instrucciones expresas de la usuaria si posteriormente cambia una regla.

## 1. Consultar primero los repositorios del profesor

Fuentes principales obligatorias:

- [Tema 06: calidad y pruebas](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026).
- [Tema 07: integración continua y despliegue](https://github.com/joseluisgs/DespliegueAplicacionesWeb-07-2025-2026).

Al comenzar una tarea de testing, leer los archivos relevantes del tema 06 y
el apartado de testing/artefactos de CI del tema 07. Para cambios en pipelines,
leer también el ejemplo práctico y los controles de calidad. Reutilizar lecturas
de esta sesión si siguen siendo suficientes; no repetirlas por cada método.

| Necesidad | Archivo del profesor y apartados |
| --- | --- |
| Unitarias, AAA, F.I.R.S.T., NUnit, Moq, cobertura | [06 / 02-Pruebas-Unitarias.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/02-Pruebas-Unitarias.md), 2.1, 2.3 y 2.4.2. |
| Integración real y ciclo de vida del contenedor | [06 / 03-Pruebas-Integracion.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/03-Pruebas-Integracion.md), 3.1 y 3.3 para .NET. |
| Colecciones, autenticación, errores y ejecución de Bruno | [06 / 04-Automatizacion-APIs.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/04-Automatizacion-APIs.md), 4.3 y 4.4; 4.7 y 4.8 si existe GraphQL. |
| Delimitar E2E de interfaz frente a E2E de API | [06 / 05-Pruebas-E2E.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-06-2025-2026/blob/master/05-Pruebas-E2E.md), 5.1. |
| Tests y artefactos en CI | [07 / 02-Integracion-Continua-CI.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-07-2025-2026/blob/master/02-Integracion-Continua-CI.md), 2.3 y 2.4. |
| Controles de calidad | [07 / 05-Control-Calidad-Branch-Protection.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-07-2025-2026/blob/master/05-Control-Calidad-Branch-Protection.md), 5.1 y 5.3. |
| Automatización práctica | [07 / 06-Pipeline-Fullstack-Practica.md](https://github.com/joseluisgs/DespliegueAplicacionesWeb-07-2025-2026/blob/master/06-Pipeline-Fullstack-Practica.md), 6.1 y 6.4. |

Abrir los archivos completos o sus apartados mediante las herramientas disponibles
de GitHub, lectura web o una copia local existente. No limitarse a ver los README
ni dar por consultada una URL por estar escrita en esta skill.

Si la página HTML impide leer el contenido, probar el enlace Raw del mismo archivo.
Si no hay acceso, indicarlo y trabajar únicamente con el código disponible y las
reglas de la usuaria. No inventar lo que supuestamente dice el profesor.

Aplicar el enfoque del profesor y comprobar la sintaxis contra las versiones
instaladas. Corregir erratas de los ejemplos y validar APIs o flags dudosos en la
documentación oficial. Distinguir en la respuesta las adaptaciones solicitadas
por la usuaria de las recomendaciones del material.

El tema 06 trata la interfaz con Playwright y recomienda Bruno para APIs.
Por petición de la usuaria, usar Bruno para los E2E de API. No presentar una
colección HTTP como prueba de interacción con botones, DOM o navegador.

Base de esta skill revisada el 18/11/2026:
tema 06, commit 0f2ea7f8f710dc6e5e06b73358f7ac2bf43c4bcf;
tema 07, commit 3ea0340f1779594e99dac8d942ae6f4229f3385d.
Los enlaces apuntan a master para consultar posibles actualizaciones.

## 2. Inspeccionar el proyecto antes de escribir tests

1. Leer las instrucciones del proyecto, solución, proyectos de test existentes,
   global.json, archivos .csproj y versiones centralizadas de paquetes.
2. Leer la implementación, interfaces, DTO, validaciones, repositorios y errores
   del comportamiento que se probará.
3. Identificar pruebas actuales, motor de base de datos, Docker/Compose, colección
   Bruno y comandos de ejecución. Conservar la organización que ya funciona.
4. Elegir el nivel de prueba y los casos útiles: éxito, errores y límites del
   contrato real. No crear los tres niveles si la petición solo requiere uno.
5. Añadir únicamente las dependencias necesarias, compatibles con el proyecto.
   Usar NUnit, NUnit3TestAdapter, Microsoft.NET.Test.Sdk y FluentAssertions cuando
   corresponda al runner; Moq solo si hace falta. No actualizar paquetes en masa.
6. Respetar Result/ROP si el proyecto lo usa: comprobar éxito/error, valor y error
   concreto. No convertir errores de dominio en excepciones para facilitar un test.

## 3. Unitarias: NUnit, FluentAssertions y Moq

- Utilizar TestFixture, Test y TestCase/TestCaseSource según corresponda.
- Separar siempre el cuerpo con los comentarios // Arrange, // Act y // Assert.
  Preparar datos y Setup en Arrange; ejecutar un comportamiento en Act;
  comprobar resultado e interacciones en Assert.
- Mantener un único comportamiento bajo prueba por caso. Permitir varias
  aserciones relacionadas con ese mismo comportamiento.
- Escribir aserciones con FluentAssertions y Should(). Para excepciones, definir
  Action o Func<Task> en Act y utilizar Throw/ThrowAsync en Assert.
- Usar async Task y await en pruebas asíncronas; evitar async void, .Wait() y .Result.
- Crear mocks y SUT nuevos en SetUp o en cada caso. Mantener independencia,
  rapidez, repetibilidad y validación automática según F.I.R.S.T.
- Aislar red, disco, reloj y bases de datos reales en unitarias. Usar datos
  deterministas y objetos reales para modelos, DTO y funciones puras.
- Nombrar los casos siguiendo Método_Escenario_ResultadoEsperado y el idioma
  del proyecto; añadir Category("Unit") a las nuevas pruebas unitarias.

### Verify obligatorio cuando se utilice Moq

- Incluir Verify explícito en la fase Assert de cada caso con Moq.
- Verificar cada mock que participa en el comportamiento comprobado. No dejar
  mocks creados por rutina: eliminar los innecesarios.
- Comprobar argumentos relevantes con valores concretos o It.Is<T>; usar
  It.IsAny<T> cuando el valor no forme parte del contrato del caso.
- Usar Times.Once, Times.Never o el número de llamadas que el contrato requiera.
- En errores, verificar que no se ejecutan efectos secundarios prohibidos.
  Por ejemplo, si falla una validación, guardar debe verificarse con Times.Never.
- No confundir Setup con Verify. MockBehavior.Strict tampoco sustituye a Verify.
  No sustituir las verificaciones explícitas por VerifyAll.
- Evitar VerifyNoOtherCalls por defecto: puede acoplar el test a detalles internos.
- Para logging ajeno al contrato, preferir NullLogger<T>. No crear un Mock solo
  para cumplir un constructor y añadir verificaciones irrelevantes.

Ejemplo ilustrativo: adaptar los tipos al código real y no crearlos en producción
solo para copiar este test.

~~~csharp
using FluentAssertions;
using Moq;
using NUnit.Framework;

[TestFixture]
[Category("Unit")]
public class ReservasServiceTests
{
    [Test]
    public async Task ReservarAsync_SinDisponibilidad_NoGuardaReserva()
    {
        // Arrange
        var repository = new Mock<IReservasRepository>();
        repository.Setup(r => r.HayDisponibilidadAsync(7))
            .ReturnsAsync(false);
        var sut = new ReservasService(repository.Object);

        // Act
        var result = await sut.ReservarAsync(7);

        // Assert
        result.Should().BeFalse();
        repository.Verify(r => r.HayDisponibilidadAsync(7), Times.Once);
        repository.Verify(r => r.GuardarAsync(7), Times.Never);
    }
}
~~~

## 4. Integración: Testcontainers para .NET y Docker

- Probar la interacción real entre componentes. Utilizar el módulo de
  Testcontainers y una imagen Docker del motor o servicio que usa el proyecto.
  No cambiar el motor de producción para acomodar un ejemplo.
- Comprobar Docker con docker info antes de ejecutar. Si no está disponible,
  indicar el bloqueo; no sustituir silenciosamente por EF InMemory ni mocks,
  ni marcar las pruebas como correctas o ignoradas.
- Arrancar el contenedor con StartAsync en OneTimeSetUp o en un fixture apropiado
  de NUnit, esperar disponibilidad y obtener su conexión/puerto dinámico.
- Inyectar la conexión del contenedor en la configuración de test antes de
  construir los servicios. Evitar que la prueba utilice una base de datos habitual.
- Ejecutar las migraciones reales cuando existan. Usar EnsureCreated únicamente
  si el proyecto no utiliza migraciones y ese es el mecanismo adecuado.
- Preparar/limpiar datos en SetUp y usar un DbContext nuevo por caso. Verificar
  persistencia con un contexto nuevo para no confundir el tracking con la BD.
- Liberar contextos y contenedores con DisposeAsync en TearDown/OneTimeTearDown,
  garantizando la limpieza aunque falle una prueba o parte del desmontaje.
- No ejecutar en paralelo contra estado mutable compartido. Aislar por BD,
  esquema o contenedor si se habilita paralelismo.
- Conservar AAA y FluentAssertions. Añadir Category("Integration").
- Usar WebApplicationFactory si procede para comprobar la API en integración,
  conectándola a los servicios reales de Testcontainers.

## 5. E2E de API: Bruno

- Arrancar la aplicación y sus dependencias reales en un entorno de prueba
  reproducible, por ejemplo con Docker Compose. Esperar un endpoint de
  disponibilidad real antes de lanzar la colección.
- Usar HTTP desde Bruno a la aplicación arrancada, incluyendo el recorrido
  API -> servicios -> persistencia. No llamar directamente al servicio desde el test.
- Crear o ampliar colecciones .bru, bruno.json y entornos .bru según el formato
  y la versión de Bruno del proyecto. Reutilizar su colección actual.
- Aplicar AAA al escenario: datos, autenticación y preparación en Arrange;
  petición HTTP en Act; comprobaciones en tests de Bruno en Assert.
  En archivos .bru, el bloque de método/URL/cuerpo representa Act.
- Utilizar el motor de aserciones JavaScript de Bruno, con test(...) y
  expect(...), res.getStatus(), res.getBody() y bru.setVar/bru.getVar según
  la documentación de la versión instalada. FluentAssertions se aplica a C#.
- Parametrizar baseUrl, credenciales y datos necesarios. Obtener tokens mediante
  login y capturar IDs de respuestas; evitar IDs preexistentes o tokens fijos.
- Comprobar código HTTP exacto, cuerpo, valores, cabeceras relevantes y errores
  de acuerdo con el contrato. No aceptar cualquier 2xx o 4xx para ocultar fallos.
- Cubrir flujos útiles, como autenticarse, crear, consultar, actualizar y borrar,
  y errores pertinentes: validación, ausencia de recurso o falta de permisos.
- Mantener cada escenario reproducible, preparar sus datos y limpiar los que
  crea. Hacer explícito el orden interno de los pasos de un mismo flujo.
- Ejecutar desde la raíz de la colección, comprobar bru --version y bru run --help,
  y adaptar los flags a esa versión. No copiar flags de los apuntes sin verificarlos.

Fuentes oficiales para resolver detalles de sintaxis:
[Bruno: uso con agentes](https://docs.usebruno.com/agents/use-cases),
[Bruno CLI](https://docs.usebruno.com/bru-cli/overview) y
[bru run](https://docs.usebruno.com/bru-cli/run/overview).

## 6. Ejecutar y comprobar

Para NUnit con VSTest, usar categorías coherentes con los atributos de las pruebas.
Adaptar proyecto, configuración y runner a lo que utilice la solución.

~~~sh
dotnet test --filter "TestCategory=Unit"
docker info
dotnet test --filter "TestCategory=Integration"
dotnet test --collect:"XPlat Code Coverage"
~~~

Ejecutar cobertura solo si procede y existe un colector compatible.
Para Bruno, situarse en la carpeta que contiene bruno.json y usar, si la CLI
instalada admite estos flags:

~~~sh
bru run --env local
~~~

- Comprobar que se descubren y ejecutan pruebas: cero pruebas no demuestra éxito.
- Leer los fallos y corregir su causa. No debilitar aserciones, eliminar Verify
  ni ignorar casos para conseguir una ejecución verde.
- No cambiar lógica de producción ajena a la petición. Si una prueba revela
  un fallo real, explicar el caso y limitar la corrección al alcance autorizado.
- Medir cobertura para encontrar comportamientos sin probar, sin inventar
  porcentajes ni imponer un umbral nuevo que la tarea no pida.
- Si no se puede ejecutar por SDK, Docker, Bruno o acceso a fuentes, informar
  exactamente de lo que se ha verificado y lo pendiente.

## 7. Aplicar el tema 07 cuando se pida CI

Adaptar el pipeline existente: restaurar/compilar, ejecutar unitarias, integración
con Docker y E2E con la aplicación disponible. Hacer que un fallo de pruebas
falle el job y conservar informes de resultados/cobertura como artefactos.
Limpiar los servicios incluso si falla el job. Verificar versiones y flags;
no copiar fragmentos de Actions sin adaptarlos al proyecto.

No modificar protecciones remotas, hacer commits, push, merges ni despliegues
sin que la usuaria lo pida expresamente.

## 8. Entregar el resultado

Indicar brevemente:

1. Qué comportamientos se cubrieron y los archivos creados o modificados.
2. Qué archivos y apartados de los repositorios del profesor se consultaron.
3. Qué comandos se ejecutaron y su resultado real.
4. Qué limitaciones o ejecuciones pendientes quedan, si las hay.

Mostrar código completo o un diff utilizable cuando se solicite para copiar
y pegar. No afirmar que una prueba pasó si no llegó a ejecutarse.
