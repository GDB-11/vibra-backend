using Vibra.Modules.Identity.Application.Errors;
using Vibra.Shared.Api;

namespace Vibra.Modules.Identity.Api.ErrorMappers;

/// <summary>
/// Mapeador de errores de <see cref="AuthController"/>. La interfaz propia evita que DI confunda
/// los mapeadores de varios controladores con el mismo tipo de error.
/// </summary>
public interface IAuthErrorMapper : IErrorHttpMapper<IdentityModuleError>;
