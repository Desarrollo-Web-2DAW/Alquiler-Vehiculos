using Alquiler_Vehiculos.Dto.Usuario;
using Alquiler_Vehiculos.Errors.Common;
using CSharpFunctionalExtensions;

namespace Alquiler_Vehiculos.Services.Usuario.Auth;

public interface IAuthService {
    Task<Result<AuthResponseDto, DomainError>> Login(LoginDto loginDto);
    
    Task<Result<AuthResponseDto, DomainError>> Signup(SignupDto signupDto);

    Task<Result<UserDto, DomainError>> GetMe(long userId);
}