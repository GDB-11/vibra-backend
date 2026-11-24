using Microsoft.Extensions.DependencyInjection;

namespace Vibra.Modules.Streaming.Api;

/// <summary>Punto de entrada del módulo Streaming.</summary>
public static class StreamingModule
{
    /// <summary>Registra los servicios del módulo Streaming.</summary>
    public static IServiceCollection AddStreamingModule(this IServiceCollection services) => services;
}
