using Alquiler_Vehiculos.Models.Usuarios;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Alquiler_Vehiculos.Tests.Unit.Infrastructure;

// Los stores solo satisfacen los constructores de los managers simulados.
// No se simula IQueryable ni EF: las consultas reales se prueban con PostgreSQL.
internal static class IdentityManagerMocks {
    internal static Mock<UserManager<User>> Usuarios() => new(
        Mock.Of<IUserStore<User>>(), Options.Create(new IdentityOptions()), new PasswordHasher<User>(),
        Array.Empty<IUserValidator<User>>(), Array.Empty<IPasswordValidator<User>>(),
        new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!, NullLogger<UserManager<User>>.Instance);

    internal static Mock<RoleManager<IdentityRole<Guid>>> Roles() => new(
        Mock.Of<IRoleStore<IdentityRole<Guid>>>(), Array.Empty<IRoleValidator<IdentityRole<Guid>>>(),
        new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), NullLogger<RoleManager<IdentityRole<Guid>>>.Instance);
}
