using Alquiler_Vehiculos.Dto.Taller;
using Alquiler_Vehiculos.Infrastructure;
using Alquiler_Vehiculos.Repositories.Taller;
using Alquiler_Vehiculos.Services.Taller;
using Alquiler_Vehiculos.Validators.Taller;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Alquiler_Vehiculos.Tests.Unit.Taller;

/// <summary>
/// Tests unitarios del registro del módulo de taller en el contenedor (TallerConfig).
/// Requisito cubierto: RF-08 (todo el módulo resoluble con una línea en Program.cs).
/// </summary>
[Category("Unit")]
public class TallerConfigTests {
    /// <summary>
    /// Dado que se registra el módulo, cuando se resuelven sus servicios,
    /// entonces quedan el repositorio, el servicio y el validador.
    /// </summary>
    [Test]
    public void AddTallerServices_RegistraRepositorioServicioYValidador() {
        // Arrange
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddTallerServices();

        // Act
        using var provider = servicios.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IParteTallerRepository>().Should().BeOfType<InMemoryParteTallerRepository>();
        provider.GetRequiredService<IParteTallerService>().Should().BeOfType<ParteTallerService>();
        provider.GetRequiredService<IValidator<AbrirParteDto>>().Should().BeOfType<AbrirParteValidator>();
    }
}
