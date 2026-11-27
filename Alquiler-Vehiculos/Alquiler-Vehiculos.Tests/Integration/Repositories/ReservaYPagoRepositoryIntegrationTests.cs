using Alquiler_Vehiculos.Entities;
using Alquiler_Vehiculos.Enums;
using Alquiler_Vehiculos.Enums.Usuarios;
using Alquiler_Vehiculos.Models.Pagos;
using Alquiler_Vehiculos.Models.Reservas;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Alquiler_Vehiculos.Repositories.Pagos;
using Alquiler_Vehiculos.Repositories.Reservas;
using Alquiler_Vehiculos.Repositories.Trabajadores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Alquiler_Vehiculos.Tests.Integration.Repositories;

/// <summary>
/// Pruebas de integración contra PostgreSQL real (Testcontainers + MigrateAsync)
/// para verificar ReservaRepository, PagoRepository y las garantías declarativas
/// de concurrencia: EXCLUDE USING gist (RF-30), xmin (15.3.4) y UX_pagos_reserva_aprobado (CU-20).
/// </summary>
[TestFixture]
[Category("Integration")]
public class ReservaYPagoRepositoryIntegrationTests {
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("test_reservas_db")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private DbContextOptions<AppDbContextUsuarios> _opciones = null!;
    private Guid _clienteId;
    private Guid _trabajadorId;

    [OneTimeSetUp]
    public async Task OneTimeSetUp() {
        await _postgres.StartAsync();

        _opciones = new DbContextOptionsBuilder<AppDbContextUsuarios>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        // MigrateAsync() aplica todas las migraciones incluyendo btree_gist y EX_reservas_vehiculo_solape (D19)
        await using var db = NuevoContexto();
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() {
        await _postgres.DisposeAsync();
    }

    [SetUp]
    public async Task SetUp() {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            TRUNCATE TABLE "pagos", "cambios_estado_reserva", "lineas_reserva", "reservas", "clientes", "trabajadores"
            RESTART IDENTITY CASCADE;
            """;
        await cmd.ExecuteNonQueryAsync();

        await using var db = NuevoContexto();
        var cliente = new Cliente {
            Id = Guid.NewGuid(),
            Nombre = "Ana",
            Apellidos = "García",
            Email = "ana.reservas@test.com",
            Telefono = "600111222",
            FechaNacimiento = new DateOnly(1995, 5, 10),
            Dni = "12345678Z"
        };
        var trabajador = new Trabajador {
            Id = Guid.NewGuid(),
            Nombre = "Carlos",
            Apellidos = "López",
            Email = "carlos.reservas@test.com",
            Telefono = "600333444",
            FechaNacimiento = new DateOnly(1990, 3, 15),
            Dni = "87654321X",
            FechaIngreso = new DateOnly(2025, 1, 10),
            Tipo = TipoTrabajador.Operaciones
        };

        db.Clientes.Add(cliente);
        db.Trabajadores.Add(trabajador);
        await db.SaveChangesAsync();

        _clienteId = cliente.Id;
        _trabajadorId = trabajador.Id;
    }

    [Test]
    public async Task SaveAsync_GuardaReservaConLineasEHistorial_YFindByIdLaRecuperaCompleta() {
        var reserva = CrearReserva(
            codigo: "RES-2026-0001",
            vehiculoId: "VEH00000001",
            inicio: new DateTime(2026, 12, 10, 10, 0, 0, DateTimeKind.Utc),
            fin: new DateTime(2026, 12, 13, 10, 0, 0, DateTimeKind.Utc));

        await using (var dbWrite = NuevoContexto()) {
            var repoWrite = CrearReservaRepo(dbWrite);
            var resultado = await repoWrite.SaveAsync(reserva);
            Assert.That(resultado, Is.EqualTo(ResultadoGuardadoReserva.Ok));
        }

        await using var dbRead = NuevoContexto();
        var repoRead = CrearReservaRepo(dbRead);
        var cargada = await repoRead.FindByIdAsync(reserva.Id);

        Assert.That(cargada, Is.Not.Null);
        Assert.That(cargada!.Codigo, Is.EqualTo("RES-2026-0001"));
        Assert.That(cargada.Cliente, Is.Not.Null);
        Assert.That(cargada.Lineas, Has.Count.EqualTo(2));
        Assert.That(cargada.HistorialEstados, Has.Count.EqualTo(1));
        Assert.That(cargada.PrecioTotal, Is.EqualTo(150m));
    }

    [Test]
    public async Task Disponibilidad_DetectaSolapesYPermiteIntervalosContiguosSemiabiertos() {
        var inicioExistente = new DateTime(2026, 12, 10, 10, 0, 0, DateTimeKind.Utc);
        var finExistente = new DateTime(2026, 12, 15, 10, 0, 0, DateTimeKind.Utc);

        var reserva = CrearReserva("RES-2026-0002", "VEH00000002", inicioExistente, finExistente);
        await using (var dbWrite = NuevoContexto()) {
            await CrearReservaRepo(dbWrite).SaveAsync(reserva);
        }

        await using var dbRead = NuevoContexto();
        var repo = CrearReservaRepo(dbRead);

        // Solape real -> ocupado
        var libreSolape = await repo.EstaVehiculoLibreAsync(
            "VEH00000002",
            new DateTime(2026, 12, 12, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 16, 10, 0, 0, DateTimeKind.Utc));
        Assert.That(libreSolape, Is.False);

        var ocupados = await repo.FindVehiculosOcupadosAsync(
            new DateTime(2026, 12, 12, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 16, 10, 0, 0, DateTimeKind.Utc));
        Assert.That(ocupados, Does.Contain("VEH00000002"));

        // Excluyendo la propia reserva (CU-14 modificar fechas) -> libre
        var libreExcluyendoPropia = await repo.EstaVehiculoLibreAsync(
            "VEH00000002",
            new DateTime(2026, 12, 10, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 18, 10, 0, 0, DateTimeKind.Utc),
            excluirReservaId: reserva.Id);
        Assert.That(libreExcluyendoPropia, Is.True);

        // Contigua exacta [15/12 10:00, 18/12 10:00) -> libre gracias al intervalo semiabierto [)
        var libreContigua = await repo.EstaVehiculoLibreAsync(
            "VEH00000002",
            finExistente,
            new DateTime(2026, 12, 18, 10, 0, 0, DateTimeKind.Utc));
        Assert.That(libreContigua, Is.True);
    }

    [Test]
    public async Task SaveAsync_DosReservasSimultaneasMismoVehiculo_SoloUnaGanaYOtraRecibeConflictoSolape() {
        var inicio = new DateTime(2026, 12, 20, 10, 0, 0, DateTimeKind.Utc);
        var fin = new DateTime(2026, 12, 23, 10, 0, 0, DateTimeKind.Utc);

        var disparo = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<ResultadoGuardadoReserva> ReservarEnContextoPropioAsync(string codigo) {
            await using var db = NuevoContexto();
            var repo = CrearReservaRepo(db);
            var reserva = CrearReserva(codigo, "VEH-RACE-01", inicio, fin);
            await disparo.Task;
            return await repo.SaveAsync(reserva);
        }

        var tareaA = ReservarEnContextoPropioAsync("RES-RACE-A");
        var tareaB = ReservarEnContextoPropioAsync("RES-RACE-B");
        disparo.SetResult();

        var resultados = await Task.WhenAll(tareaA, tareaB);

        Assert.That(resultados.Count(r => r == ResultadoGuardadoReserva.Ok), Is.EqualTo(1));
        Assert.That(resultados.Count(r => r == ResultadoGuardadoReserva.ConflictoSolape), Is.EqualTo(1));
    }

    [Test]
    public async Task SaveAsync_ReservaCancelada_LiberaVehiculoEnPostgreSqlParaMismasFechas() {
        var inicio = new DateTime(2026, 12, 24, 10, 0, 0, DateTimeKind.Utc);
        var fin = new DateTime(2026, 12, 27, 10, 0, 0, DateTimeKind.Utc);
        var reservaA = CrearReserva("RES-CU15-A", "VEH-CU15-01", inicio, fin);

        await using (var db1 = NuevoContexto()) {
            Assert.That(await CrearReservaRepo(db1).SaveAsync(reservaA), Is.EqualTo(ResultadoGuardadoReserva.Ok));
        }

        // A cancela su reserva -> el filtro parcial WHERE Estado IN (...) deja de bloquear
        await using (var db2 = NuevoContexto()) {
            var repo2 = CrearReservaRepo(db2);
            var cargada = await repo2.FindByIdAsync(reservaA.Id, tracking: true);
            cargada!.Estado = EstadoReserva.Cancelada;
            Assert.That(await repo2.UpdateAsync(cargada), Is.EqualTo(ResultadoGuardadoReserva.Ok));
        }

        // B puede reservar el mismo vehículo en las mismas fechas sin tocar nada a mano (CU-15)
        await using var db3 = NuevoContexto();
        var reservaB = CrearReserva("RES-CU15-B", "VEH-CU15-01", inicio, fin);
        var resultadoB = await CrearReservaRepo(db3).SaveAsync(reservaB);
        Assert.That(resultadoB, Is.EqualTo(ResultadoGuardadoReserva.Ok));
    }

    [Test]
    public async Task UpdateAsync_DosContextosModificanMismaReserva_SegundoRecibeConflictoVersionXmin() {
        var reserva = CrearReserva(
            "RES-XMIN-01",
            "VEH-XMIN-01",
            new DateTime(2026, 12, 28, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 30, 10, 0, 0, DateTimeKind.Utc));

        await using (var dbInit = NuevoContexto()) {
            await CrearReservaRepo(dbInit).SaveAsync(reserva);
        }

        // Apuntes 15.3.4: dos contextos distintos leen la misma fila con el mismo xmin
        await using var ctxA = NuevoContexto();
        await using var ctxB = NuevoContexto();
        var repoA = CrearReservaRepo(ctxA);
        var repoB = CrearReservaRepo(ctxB);

        var reservaA = await repoA.FindByIdAsync(reserva.Id, tracking: true);
        var reservaB = await repoB.FindByIdAsync(reserva.Id, tracking: true);

        reservaA!.Estado = EstadoReserva.Confirmada;
        var resA = await repoA.UpdateAsync(reservaA);

        reservaB!.Estado = EstadoReserva.Caducada;
        var resB = await repoB.UpdateAsync(reservaB);

        Assert.That(resA, Is.EqualTo(ResultadoGuardadoReserva.Ok));
        Assert.That(resB, Is.EqualTo(ResultadoGuardadoReserva.ConflictoVersion));
    }

    [Test]
    public async Task FindPendientesCaducadasAsync_DevuelveSoloPendientesExpiradas() {
        var ahora = new DateTime(2026, 12, 1, 12, 0, 0, DateTimeKind.Utc);
        var caducada = CrearReserva("RES-CAD-1", "VEH-CAD-001", ahora.AddDays(2), ahora.AddDays(4));
        caducada.ExpiraEn = ahora.AddMinutes(-5);

        var vigente = CrearReserva("RES-CAD-2", "VEH-CAD-002", ahora.AddDays(2), ahora.AddDays(4));
        vigente.ExpiraEn = ahora.AddMinutes(20);

        var confirmada = CrearReserva("RES-CAD-3", "VEH-CAD-003", ahora.AddDays(2), ahora.AddDays(4));
        confirmada.Estado = EstadoReserva.Confirmada;
        confirmada.ExpiraEn = ahora.AddMinutes(-10);

        await using (var dbWrite = NuevoContexto()) {
            var repo = CrearReservaRepo(dbWrite);
            await repo.SaveAsync(caducada);
            await repo.SaveAsync(vigente);
            await repo.SaveAsync(confirmada);
        }

        await using var dbRead = NuevoContexto();
        var resultado = (await CrearReservaRepo(dbRead).FindPendientesCaducadasAsync(ahora)).ToList();

        Assert.That(resultado, Has.Count.EqualTo(1));
        Assert.That(resultado[0].Id, Is.EqualTo(caducada.Id));
    }

    [Test]
    public async Task ReservasVivas_DetectaClienteTrabajadorYVehiculoSoloEnEstadosVivos() {
        var reserva = CrearReserva(
            "RES-VIVA-01",
            "VEH-VIVA-01",
            new DateTime(2026, 12, 5, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 8, 10, 0, 0, DateTimeKind.Utc));
        reserva.Estado = EstadoReserva.Confirmada;

        await using (var dbWrite = NuevoContexto()) {
            await CrearReservaRepo(dbWrite).SaveAsync(reserva);
        }

        await using (var dbCheck = NuevoContexto()) {
            var reservaRepo = CrearReservaRepo(dbCheck);
            var trabajadorRepo = new TrabajadorRepository(dbCheck, NullLogger<TrabajadorRepository>.Instance);

            Assert.That(await reservaRepo.ClienteTieneReservasVivasAsync(_clienteId), Is.True);
            Assert.That(await reservaRepo.TrabajadorTieneReservasVivasAsync(_trabajadorId), Is.True);
            Assert.That(await trabajadorRepo.TieneReservasVivasAsync(_trabajadorId), Is.True);
            Assert.That(await reservaRepo.VehiculoTieneReservasVivasAsync("VEH-VIVA-01"), Is.True);
        }

        // Al finalizar, ya no bloquea baja de cliente, trabajador ni vehículo
        await using (var dbUpdate = NuevoContexto()) {
            var repo = CrearReservaRepo(dbUpdate);
            var r = await repo.FindByIdAsync(reserva.Id, tracking: true);
            r!.Estado = EstadoReserva.Finalizada;
            await repo.UpdateAsync(r);
        }

        await using var dbFinal = NuevoContexto();
        var repoFinal = CrearReservaRepo(dbFinal);
        var trabRepoFinal = new TrabajadorRepository(dbFinal, NullLogger<TrabajadorRepository>.Instance);

        Assert.That(await repoFinal.ClienteTieneReservasVivasAsync(_clienteId), Is.False);
        Assert.That(await trabRepoFinal.TieneReservasVivasAsync(_trabajadorId), Is.False);
        Assert.That(await repoFinal.VehiculoTieneReservasVivasAsync("VEH-VIVA-01"), Is.False);
    }

    [Test]
    public async Task PagoRepository_DosPagosAprobadosSimultaneos_SoloUnoGanaYOtroBloqueadoPorIndiceUnico() {
        var reserva = CrearReserva(
            "RES-CU20-01",
            "VEH-CU20-01",
            new DateTime(2026, 12, 10, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 12, 10, 0, 0, DateTimeKind.Utc));

        await using (var dbInit = NuevoContexto()) {
            await CrearReservaRepo(dbInit).SaveAsync(reserva);
        }

        var disparo = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<(bool Guardado, bool ConflictoDobleCobro)> PagarEnContextoPropioAsync(string refPasarela) {
            await using var db = NuevoContexto();
            var repo = CrearPagoRepo(db);
            var pago = CrearPago(reserva.Id, EstadoPago.Aprobado, refPasarela);
            await disparo.Task;
            return await repo.SaveAsync(pago);
        }

        var tareaA = PagarEnContextoPropioAsync("REF-SIM-A");
        var tareaB = PagarEnContextoPropioAsync("REF-SIM-B");
        disparo.SetResult();

        var resultados = await Task.WhenAll(tareaA, tareaB);

        Assert.That(resultados.Count(r => r.Guardado && !r.ConflictoDobleCobro), Is.EqualTo(1));
        Assert.That(resultados.Count(r => !r.Guardado && r.ConflictoDobleCobro), Is.EqualTo(1));
    }

    [Test]
    public async Task PagoRepository_MultiplesPagosRechazados_PermiteReintentarYAprobar() {
        var reserva = CrearReserva(
            "RES-CU11-01",
            "VEH-CU11-01",
            new DateTime(2026, 12, 14, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 16, 10, 0, 0, DateTimeKind.Utc));

        await using (var dbInit = NuevoContexto()) {
            await CrearReservaRepo(dbInit).SaveAsync(reserva);
        }

        await using var db = NuevoContexto();
        var repo = CrearPagoRepo(db);

        var rechazo1 = await repo.SaveAsync(CrearPago(reserva.Id, EstadoPago.Rechazado, "REF-REJ-1", "Fondos insuficientes"));
        var rechazo2 = await repo.SaveAsync(CrearPago(reserva.Id, EstadoPago.Rechazado, "REF-REJ-2", "Tarjeta bloqueada"));
        var aprobado = await repo.SaveAsync(CrearPago(reserva.Id, EstadoPago.Aprobado, "REF-OK-1"));

        Assert.That(rechazo1.Guardado, Is.True);
        Assert.That(rechazo2.Guardado, Is.True);
        Assert.That(aprobado.Guardado, Is.True);
        Assert.That(await repo.ExistsPagoAprobadoAsync(reserva.Id), Is.True);

        var historial = (await repo.FindByReservaIdAsync(reserva.Id)).ToList();
        Assert.That(historial, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task FindAllAsync_YFindAllAsNoTracking_ExcluyenBajaLogicaYOrdenanDescendente() {
        var antigua = CrearReserva("RES-ALL-1", "VEH-ALL-001", new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 3, 10, 0, 0, DateTimeKind.Utc));
        antigua.CreatedAt = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

        var reciente = CrearReserva("RES-ALL-2", "VEH-ALL-002", new DateTime(2026, 12, 4, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 6, 10, 0, 0, DateTimeKind.Utc));
        reciente.CreatedAt = new DateTime(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc);
        reciente.Estado = EstadoReserva.Confirmada;

        var borrada = CrearReserva("RES-ALL-3", "VEH-ALL-003", new DateTime(2026, 12, 7, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 9, 10, 0, 0, DateTimeKind.Utc));
        borrada.IsDeleted = true;
        borrada.DeletedAt = DateTime.UtcNow;

        await using (var dbWrite = NuevoContexto()) {
            var repo = CrearReservaRepo(dbWrite);
            await repo.SaveAsync(antigua);
            await repo.SaveAsync(reciente);
            await repo.SaveAsync(borrada);
        }

        await using var dbRead = NuevoContexto();
        var repoRead = CrearReservaRepo(dbRead);

        var todas = (await repoRead.FindAllAsync()).ToList();
        Assert.That(todas, Has.Count.EqualTo(2));
        Assert.That(todas[0].Codigo, Is.EqualTo("RES-ALL-2"));
        Assert.That(todas[1].Codigo, Is.EqualTo("RES-ALL-1"));

        var soloConfirmadas = await repoRead.FindAllAsNoTracking()
            .Where(r => r.Estado == EstadoReserva.Confirmada)
            .ToListAsync();
        Assert.That(soloConfirmadas, Has.Count.EqualTo(1));
        Assert.That(soloConfirmadas[0].Codigo, Is.EqualTo("RES-ALL-2"));
    }

    [Test]
    public async Task FindByClienteIdAsync_DevuelveSoloLasDelClienteYExcluyeBajaLogica() {
        var otroCliente = new Cliente {
            Id = Guid.NewGuid(),
            Nombre = "Luis",
            Apellidos = "Pérez",
            Email = "luis.otro@test.com",
            Telefono = "600999888",
            FechaNacimiento = new DateOnly(1992, 8, 20),
            Dni = "23456789D"
        };
        await using (var dbCli = NuevoContexto()) {
            dbCli.Clientes.Add(otroCliente);
            await dbCli.SaveChangesAsync();
        }

        var mia = CrearReserva("RES-CLI-1", "VEH-CLI-001", new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 3, 10, 0, 0, DateTimeKind.Utc));
        var miaBorrada = CrearReserva("RES-CLI-2", "VEH-CLI-002", new DateTime(2026, 12, 4, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 6, 10, 0, 0, DateTimeKind.Utc));
        miaBorrada.IsDeleted = true;

        var deOtro = CrearReserva("RES-CLI-3", "VEH-CLI-003", new DateTime(2026, 12, 7, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 9, 10, 0, 0, DateTimeKind.Utc));
        deOtro.ClienteId = otroCliente.Id;

        await using (var dbWrite = NuevoContexto()) {
            var repo = CrearReservaRepo(dbWrite);
            await repo.SaveAsync(mia);
            await repo.SaveAsync(miaBorrada);
            await repo.SaveAsync(deOtro);
        }

        await using var dbRead = NuevoContexto();
        var resultado = (await CrearReservaRepo(dbRead).FindByClienteIdAsync(_clienteId)).ToList();

        Assert.That(resultado, Has.Count.EqualTo(1));
        Assert.That(resultado[0].Codigo, Is.EqualTo("RES-CLI-1"));
    }

    [Test]
    public async Task FindByIdAsync_YFindByCodigoAsync_DevuelvenNullSiNoExisteOEstaBorrada() {
        var borrada = CrearReserva("RES-DEL-01", "VEH-DEL-001", new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 3, 10, 0, 0, DateTimeKind.Utc));
        borrada.IsDeleted = true;

        var activa = CrearReserva("RES-OK-01", "VEH-OK-0001", new DateTime(2026, 12, 4, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 6, 10, 0, 0, DateTimeKind.Utc));

        await using (var dbWrite = NuevoContexto()) {
            var repo = CrearReservaRepo(dbWrite);
            await repo.SaveAsync(borrada);
            await repo.SaveAsync(activa);
        }

        await using var dbRead = NuevoContexto();
        var repoRead = CrearReservaRepo(dbRead);

        Assert.That(await repoRead.FindByIdAsync(Guid.NewGuid()), Is.Null);
        Assert.That(await repoRead.FindByIdAsync(borrada.Id), Is.Null);
        Assert.That(await repoRead.FindByCodigoAsync("NO-EXISTE"), Is.Null);
        Assert.That(await repoRead.FindByCodigoAsync("RES-DEL-01"), Is.Null);

        var encontrada = await repoRead.FindByCodigoAsync("RES-OK-01");
        Assert.That(encontrada, Is.Not.Null);
        Assert.That(encontrada!.Id, Is.EqualTo(activa.Id));
        Assert.That(encontrada.Lineas, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task UpdateAsync_AlargarFechasConSolape_DevuelveConflictoSolapeYDejaOriginalIntacta() {
        var reservaA = CrearReserva(
            "RES-CU14-A",
            "VEH-CU14-01",
            new DateTime(2026, 12, 10, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 13, 10, 0, 0, DateTimeKind.Utc));
        var reservaB = CrearReserva(
            "RES-CU14-B",
            "VEH-CU14-01",
            new DateTime(2026, 12, 15, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 12, 18, 10, 0, 0, DateTimeKind.Utc));

        await using (var dbInit = NuevoContexto()) {
            var repo = CrearReservaRepo(dbInit);
            Assert.That(await repo.SaveAsync(reservaA), Is.EqualTo(ResultadoGuardadoReserva.Ok));
            Assert.That(await repo.SaveAsync(reservaB), Is.EqualTo(ResultadoGuardadoReserva.Ok));
        }

        // CU-14: A intenta alargar hasta el 16 -> choca con B [15, 18) en EX_reservas_vehiculo_solape
        await using (var dbUpdate = NuevoContexto()) {
            var repo = CrearReservaRepo(dbUpdate);
            var cargada = await repo.FindByIdAsync(reservaA.Id, tracking: true);
            cargada!.FechaDevolucion = new DateTime(2026, 12, 16, 10, 0, 0, DateTimeKind.Utc);
            var resultado = await repo.UpdateAsync(cargada);
            Assert.That(resultado, Is.EqualTo(ResultadoGuardadoReserva.ConflictoSolape));
        }

        // La reserva original sigue intacta con fecha devolución 13/12
        await using var dbVerify = NuevoContexto();
        var verificada = await CrearReservaRepo(dbVerify).FindByIdAsync(reservaA.Id);
        Assert.That(verificada!.FechaDevolucion, Is.EqualTo(new DateTime(2026, 12, 13, 10, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public async Task SaveAsync_EstadosCaducadaNoPresentadoYBorradoLogico_NoBloqueanVehiculo() {
        var inicio = new DateTime(2026, 12, 20, 10, 0, 0, DateTimeKind.Utc);
        var fin = new DateTime(2026, 12, 23, 10, 0, 0, DateTimeKind.Utc);

        var caducada = CrearReserva("RES-EST-1", "VEH-EST-001", inicio, fin);
        caducada.Estado = EstadoReserva.Caducada;

        var noPresentado = CrearReserva("RES-EST-2", "VEH-EST-001", inicio, fin);
        noPresentado.Estado = EstadoReserva.NoPresentado;

        var borrada = CrearReserva("RES-EST-3", "VEH-EST-001", inicio, fin);
        borrada.Estado = EstadoReserva.Confirmada;
        borrada.IsDeleted = true;

        var nuevaViva = CrearReserva("RES-EST-4", "VEH-EST-001", inicio, fin);

        await using var db = NuevoContexto();
        var repo = CrearReservaRepo(db);

        Assert.That(await repo.SaveAsync(caducada), Is.EqualTo(ResultadoGuardadoReserva.Ok));
        Assert.That(await repo.SaveAsync(noPresentado), Is.EqualTo(ResultadoGuardadoReserva.Ok));
        Assert.That(await repo.SaveAsync(borrada), Is.EqualTo(ResultadoGuardadoReserva.Ok));
        Assert.That(await repo.SaveAsync(nuevaViva), Is.EqualTo(ResultadoGuardadoReserva.Ok));
    }

    [Test]
    public async Task VehiculoTieneReservasVivasAsync_RespetaExcluirReservaIdParaRF33() {
        var reserva1 = CrearReserva("RES-RF33-1", "VEH-RF33-01", new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 3, 10, 0, 0, DateTimeKind.Utc));
        reserva1.Estado = EstadoReserva.EnCurso;

        await using (var db1 = NuevoContexto()) {
            await CrearReservaRepo(db1).SaveAsync(reserva1);
        }

        // Solo existe reserva1 -> al excluirla, el vehículo NO tiene otras reservas vivas (pasará a Disponible)
        await using (var dbCheck1 = NuevoContexto()) {
            var tieneOtras = await CrearReservaRepo(dbCheck1)
                .VehiculoTieneReservasVivasAsync("VEH-RF33-01", excluirReservaId: reserva1.Id);
            Assert.That(tieneOtras, Is.False);
        }

        // Añadimos una reserva futura Confirmada -> al excluir reserva1, SÍ tiene otra viva (pasará a Reservado)
        var reserva2 = CrearReserva("RES-RF33-2", "VEH-RF33-01", new DateTime(2026, 12, 10, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 12, 10, 0, 0, DateTimeKind.Utc));
        reserva2.Estado = EstadoReserva.Confirmada;

        await using (var db2 = NuevoContexto()) {
            await CrearReservaRepo(db2).SaveAsync(reserva2);
        }

        await using var dbCheck2 = NuevoContexto();
        var tieneOtrasDespues = await CrearReservaRepo(dbCheck2)
            .VehiculoTieneReservasVivasAsync("VEH-RF33-01", excluirReservaId: reserva1.Id);
        Assert.That(tieneOtrasDespues, Is.True);
    }

    [Test]
    public async Task BeginTransactionAsync_RollbackDeshaceReservaYPagoDeFormaAtomica() {
        var reserva = CrearReserva("RES-TX-01", "VEH-TX-0001", new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 3, 10, 0, 0, DateTimeKind.Utc));

        await using (var dbTx = NuevoContexto()) {
            var reservaRepo = CrearReservaRepo(dbTx);
            var pagoRepo = CrearPagoRepo(dbTx);

            await using var tx = await reservaRepo.BeginTransactionAsync();
            Assert.That(await reservaRepo.SaveAsync(reserva), Is.EqualTo(ResultadoGuardadoReserva.Ok));
            var (guardado, _) = await pagoRepo.SaveAsync(CrearPago(reserva.Id, EstadoPago.Aprobado, "REF-TX-1"));
            Assert.That(guardado, Is.True);

            await tx.RollbackAsync();
        }

        // Tras el rollback, ni la reserva ni el pago existen en PostgreSQL
        await using var dbVerify = NuevoContexto();
        Assert.That(await CrearReservaRepo(dbVerify).FindByIdAsync(reserva.Id), Is.Null);
        Assert.That(await CrearPagoRepo(dbVerify).ExistsPagoAprobadoAsync(reserva.Id), Is.False);
    }

    [Test]
    public async Task PagoRepository_FindPagoAprobadoYUpdateAsync_PermiteReembolsarCorrectamente() {
        var reserva = CrearReserva("RES-CU09-01", "VEH-CU09-01", new DateTime(2026, 12, 18, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 21, 10, 0, 0, DateTimeKind.Utc));

        await using (var dbInit = NuevoContexto()) {
            await CrearReservaRepo(dbInit).SaveAsync(reserva);
            await CrearPagoRepo(dbInit).SaveAsync(CrearPago(reserva.Id, EstadoPago.Aprobado, "REF-CU09-1"));
        }

        // CU-09 / CU-13: recuperar el pago aprobado con tracking y pasarlo a Reembolsado
        await using (var dbRefund = NuevoContexto()) {
            var repo = CrearPagoRepo(dbRefund);
            var pago = await repo.FindPagoAprobadoAsync(reserva.Id, tracking: true);
            Assert.That(pago, Is.Not.Null);

            pago!.Estado = EstadoPago.Reembolsado;
            pago.ImporteReembolsado = 150m;
            await repo.UpdateAsync(pago);
        }

        await using var dbVerify = NuevoContexto();
        var repoVerify = CrearPagoRepo(dbVerify);
        Assert.That(await repoVerify.ExistsPagoAprobadoAsync(reserva.Id), Is.False);
        Assert.That(await repoVerify.FindPagoAprobadoAsync(reserva.Id), Is.Null);

        var pagos = (await repoVerify.FindByReservaIdAsync(reserva.Id)).ToList();
        Assert.That(pagos, Has.Count.EqualTo(1));
        Assert.That(pagos[0].Estado, Is.EqualTo(EstadoPago.Reembolsado));
        Assert.That(pagos[0].ImporteReembolsado, Is.EqualTo(150m));
    }

    private AppDbContextUsuarios NuevoContexto() => new(_opciones);

    private static ReservaRepository CrearReservaRepo(AppDbContextUsuarios db) =>
        new(db, NullLogger<ReservaRepository>.Instance);

    private static PagoRepository CrearPagoRepo(AppDbContextUsuarios db) =>
        new(db, NullLogger<PagoRepository>.Instance);

    private Reserva CrearReserva(string codigo, string vehiculoId, DateTime inicio, DateTime fin) => new() {
        Id = Guid.NewGuid(),
        Codigo = codigo,
        ClienteId = _clienteId,
        TrabajadorId = _trabajadorId,
        VehiculoId = vehiculoId,
        FechaRecogida = inicio,
        FechaDevolucion = fin,
        ExpiraEn = DateTime.UtcNow.AddMinutes(30),
        Estado = EstadoReserva.Pendiente,
        PrecioBaseDiaCongelado = 40m,
        NombrePolizaCongelada = "Básica",
        IncrementoPolizaDiaCongelado = 10m,
        PrecioTotal = 150m,
        Lineas = [
            new LineaReserva {
                Tipo = TipoLineaReserva.Vehiculo,
                Concepto = "Vehículo base (3 días)",
                Cantidad = 3,
                PrecioUnitario = 40m,
                Subtotal = 120m
            },
            new LineaReserva {
                Tipo = TipoLineaReserva.Poliza,
                Concepto = "Póliza Básica (3 días)",
                Cantidad = 3,
                PrecioUnitario = 10m,
                Subtotal = 30m
            }
        ],
        HistorialEstados = [
            new CambioEstadoReserva {
                EstadoAnterior = null,
                EstadoNuevo = EstadoReserva.Pendiente,
                Motivo = "Alta de reserva"
            }
        ]
    };

    private static Pago CrearPago(Guid reservaId, EstadoPago estado, string referencia, string? motivoRechazo = null) => new() {
        Id = Guid.NewGuid(),
        ReservaId = reservaId,
        Importe = 150m,
        Estado = estado,
        MarcaTarjeta = "Visa",
        UltimosCuatroDigitos = "8713",
        NumeroTarjeta = "cifrado-aes-gcm-simulado",
        ReferenciaPasarela = referencia,
        MotivoRechazo = motivoRechazo
    };
}
