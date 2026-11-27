using Microsoft.Extensions.DependencyInjection;
using Vibra.Shared.Infrastructure.Migrations;

namespace Vibra.Modules.Streaming.Api;

/// <summary>Punto de entrada del módulo Streaming.</summary>
public static class StreamingModule
{
    /// <summary>Migraciones del módulo Streaming.</summary>
    public static ModuleMigrations Migrations { get; } = new("streaming", typeof(StreamingModule).Assembly);

    /// <summary>Registra los servicios del módulo Streaming.</summary>
    public static IServiceCollection AddStreamingModule(this IServiceCollection services) => services;
}
