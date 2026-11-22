using Alquiler_Vehiculos.Cache;
using FluentAssertions;

namespace Alquiler_Vehiculos.Tests.Unit.Cache;

/// <summary>
/// Tests unitarios de la política pura de la caché (TTL mixto y expulsión LRU).
/// Requisito cubierto: RNF-02.
/// </summary>
[Category("Unit")]
public class CachePolicyTests {
    private static readonly DateTimeOffset Ahora = new(2026, 11, 17, 10, 0, 0, TimeSpan.Zero);

    #region Tests ComputeDeadline

    /// <summary>
    /// Dado que se crea una entrada, cuando se calcula su deadline,
    /// entonces es el momento actual más la vida máxima.
    /// </summary>
    [Test]
    public void ComputeDeadline_EntradaNueva_EsAhoraMasLaVidaMaxima() {
        // Arrange
        var vidaMaxima = TimeSpan.FromMinutes(10);

        // Act
        var deadline = CachePolicy.ComputeDeadline(Ahora, vidaMaxima);

        // Assert
        deadline.Should().Be(Ahora.AddMinutes(10));
    }

    #endregion

    #region Tests EffectiveTtl

    /// <summary>
    /// Dado que a una entrada le queda más vida que la ventana móvil,
    /// cuando se calcula su TTL efectivo, entonces es la ventana móvil.
    /// </summary>
    [Test]
    public void EffectiveTtl_QuedaMasVidaQueLaVentana_DevuelveLaVentanaMovil() {
        // Arrange
        var deadline = Ahora.AddMinutes(10);

        // Act
        var ttl = CachePolicy.EffectiveTtl(Ahora, deadline, TimeSpan.FromMinutes(1));

        // Assert
        ttl.Should().Be(TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Dado que a una entrada le queda menos vida que la ventana móvil,
    /// cuando se calcula su TTL efectivo, entonces se recorta al deadline
    /// y la renovación nunca rebasa la vida máxima.
    /// </summary>
    [Test]
    public void EffectiveTtl_QuedaMenosVidaQueLaVentana_RecortaAlDeadline() {
        // Arrange
        var deadline = Ahora.AddSeconds(30);

        // Act
        var ttl = CachePolicy.EffectiveTtl(Ahora, deadline, TimeSpan.FromMinutes(1));

        // Assert
        ttl.Should().Be(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Dado que una entrada ya superó su deadline,
    /// cuando se calcula su TTL efectivo, entonces es cero.
    /// </summary>
    [Test]
    public void EffectiveTtl_DeadlineYaPasado_DevuelveCero() {
        // Arrange
        var deadline = Ahora.AddSeconds(-1);

        // Act
        var ttl = CachePolicy.EffectiveTtl(Ahora, deadline, TimeSpan.FromMinutes(1));

        // Assert
        ttl.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region Tests IsExpired

    /// <summary>
    /// Dado que una entrada está viva, cuando se comprueba su caducidad,
    /// entonces no está caducada antes del deadline ni en el instante anterior.
    /// </summary>
    [Test]
    public void IsExpired_AntesDelDeadline_DevuelveFalso() {
        // Arrange
        var deadline = Ahora.AddMinutes(10);

        // Act & Assert
        CachePolicy.IsExpired(Ahora, deadline).Should().BeFalse();
        CachePolicy.IsExpired(Ahora.AddSeconds(599), deadline).Should().BeFalse();
    }

    /// <summary>
    /// Dado que una entrada llega a su deadline, cuando se comprueba su caducidad,
    /// entonces está caducada justo en el deadline y después.
    /// </summary>
    [Test]
    public void IsExpired_DesdeElDeadline_DevuelveCierto() {
        // Arrange
        var deadline = Ahora.AddMinutes(10);

        // Act & Assert
        CachePolicy.IsExpired(deadline, deadline).Should().BeTrue();
        CachePolicy.IsExpired(deadline.AddSeconds(1), deadline).Should().BeTrue();
    }

    #endregion

    #region Tests EvictionCount

    /// <summary>
    /// Dado que la caché no supera su capacidad, cuando se calcula la expulsión,
    /// entonces no hay que expulsar nada.
    /// </summary>
    [Test]
    public void EvictionCount_DentroDeLaCapacidad_DevuelveCero() {
        // Arrange & Act & Assert
        CachePolicy.EvictionCount(3, capacity: 3).Should().Be(0);
        CachePolicy.EvictionCount(2, capacity: 3).Should().Be(0);
    }

    /// <summary>
    /// Dado que la caché supera su capacidad, cuando se calcula la expulsión,
    /// entonces hay que expulsar exactamente el exceso.
    /// </summary>
    [Test]
    public void EvictionCount_SuperaLaCapacidad_DevuelveElExceso() {
        // Arrange & Act & Assert
        CachePolicy.EvictionCount(4, capacity: 3).Should().Be(1);
        CachePolicy.EvictionCount(6, capacity: 3).Should().Be(3);
    }

    #endregion
}
