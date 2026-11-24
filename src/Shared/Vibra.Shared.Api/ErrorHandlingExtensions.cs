using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Vibra.Shared.Api;

/// <summary>
/// Seguridad de errores para todo el host: lo que no pasa por un <c>Result</c>
/// (excepciones, rutas inexistentes, JSON inválido) también responde con Problem Details.
/// </summary>
public static class ErrorHandlingExtensions
{
    /// <summary>Registra los controladores y Problem Details.</summary>
    public static IMvcBuilder AddVibraControllers(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails =
            context => Translate(context.ProblemDetails));

        return services
            .AddControllers()
            // Evita que los errores de deserialización revelen nombres de tipos de .NET.
            .AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false);
    }

    // Solo afecta a los problemas que genera ASP.NET Core, no a los de los mapeadores.
    private static void Translate(ProblemDetails problem)
    {
        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            var generic = Problems.Unexpected();
            problem.Type = generic.Type;
            problem.Title = generic.Title;
            problem.Detail = generic.Detail;
            return;
        }

        problem.Title = problem.Status switch
        {
            StatusCodes.Status400BadRequest => "Solicitud no válida",
            StatusCodes.Status401Unauthorized => "No autorizado",
            StatusCodes.Status403Forbidden => "Acceso denegado",
            StatusCodes.Status404NotFound => "No encontrado",
            StatusCodes.Status405MethodNotAllowed => "Método no permitido",
            StatusCodes.Status415UnsupportedMediaType => "Tipo de contenido no admitido",
            _ => problem.Title,
        };
    }

    /// <summary>Debe ser el primer middleware del host.</summary>
    public static IApplicationBuilder UseVibraErrorHandling(this IApplicationBuilder app) =>
        app.UseExceptionHandler()
            .UseStatusCodePages();
}
