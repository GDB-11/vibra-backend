using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vibra.Modules.Identity.Api.ErrorMappers;
using Vibra.Modules.Identity.Api.Validators;
using Vibra.Modules.Identity.Application.Contracts;
using Vibra.Modules.Identity.Application.Errors;
using Vibra.Modules.Identity.Application.Repositories;
using Vibra.Modules.Identity.Application.Security;
using Vibra.Modules.Identity.Application.Services;
using Vibra.Modules.Identity.Domain;
using Vibra.Modules.Identity.Infrastructure.Repositories;
using Vibra.Modules.Identity.Infrastructure.Security;
using Vibra.Shared.Api;
using Vibra.Shared.Infrastructure.Migrations;

namespace Vibra.Modules.Identity.Api;

/// <summary>Punto de entrada del módulo Identity.</summary>
public static class IdentityModule
{
    /// <summary>Migraciones del módulo Identity.</summary>
    public static ModuleMigrations Migrations { get; } = new("identity", typeof(IdentityModule).Assembly);

    /// <summary>Registra los servicios del módulo Identity y la autenticación con JWT.</summary>
    /// <param name="validateOnStart">
    /// Sin una clave válida, la API no arranca. Solo la exportación de OpenAPI, que no tiene secretos, lo desactiva.
    /// </param>
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, bool validateOnStart = true)
    {
        var jwt = services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.Section)
            .ValidateDataAnnotations();

        if (validateOnStart)
            jwt.ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearer>();
        services.AddAuthorization();

        // Contraseña y bloqueo usan los valores predeterminados de Identity.
        // AspNetUserManager cancela con la solicitud HTTP.
        services.AddHttpContextAccessor();
        services.AddIdentityCore<User>(options => options.User.AllowedUserNameCharacters = string.Empty)
            .AddUserManager<AspNetUserManager<User>>()
            .AddUserStore<UserStore>()
            .AddErrorDescriber<SpanishIdentityErrorDescriber>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddSingleton<ITokenIssuer, TokenIssuer>();
        services.AddSingleton<DummyPassword>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IAuthErrorMapper, AuthErrorMapper>();
        services.AddSingleton<IRequestValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddSingleton<IRequestValidator<LoginRequest>, LoginRequestValidator>();
        services.AddSingleton<IRequestValidator<RefreshRequest>, RefreshRequestValidator>();

        return services;
    }
}
