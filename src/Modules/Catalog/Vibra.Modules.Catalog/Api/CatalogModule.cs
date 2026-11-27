using Microsoft.Extensions.DependencyInjection;
using Vibra.Modules.Catalog.Api.ErrorMappers;
using Vibra.Modules.Catalog.Application.Repositories;
using Vibra.Modules.Catalog.Application.Services;
using Vibra.Modules.Catalog.Application.Storage;
using Vibra.Modules.Catalog.Infrastructure.Repositories;
using Vibra.Modules.Catalog.Infrastructure.Storage;
using Vibra.Shared.Infrastructure.Migrations;

namespace Vibra.Modules.Catalog.Api;

/// <summary>Punto de entrada del módulo Catalog.</summary>
public static class CatalogModule
{
    /// <summary>Migraciones del módulo Catalog.</summary>
    public static ModuleMigrations Migrations { get; } = new("catalog", typeof(CatalogModule).Assembly);

    /// <summary>Registra los servicios del módulo Catalog.</summary>
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddOptions<ImageSettings>()
            .BindConfiguration(ImageSettings.Section)
            .Validate(settings => settings.HasValidBaseUrl(), "Images:BaseUrl debe ser una URL https sin consulta.")
            .Validate(settings => settings.Widths.All(width => width > 0), "Images:CoverWidths debe ser positivo.")
            .ValidateOnStart();

        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IAlbumRepository, AlbumRepository>();
        services.AddScoped<ITrackRepository, TrackRepository>();
        services.AddSingleton<ICoverUrlBuilder, CoverUrlBuilder>();
        services.AddScoped<IArtistService, ArtistService>();
        services.AddScoped<IAlbumService, AlbumService>();
        services.AddScoped<ITrackService, TrackService>();
        services.AddSingleton<IArtistErrorMapper, ArtistErrorMapper>();
        services.AddSingleton<IAlbumErrorMapper, AlbumErrorMapper>();
        services.AddSingleton<ITrackErrorMapper, TrackErrorMapper>();

        return services;
    }
}
