using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Vibra.Shared.Api;

/// <summary>
/// Añade un ETag fuerte a las respuestas 200 y responde 304 si coincide con <c>If-None-Match</c>.
/// Ahorra transferencia, no consultas: la respuesta se calcula siempre.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ETagAttribute : Attribute, IAsyncResultFilter
{
    // El cliente puede guardar la respuesta, pero debe revalidarla; ninguna caché compartida.
    private const string CacheControl = "private, no-cache";
    private const string ContentType = "application/json; charset=utf-8";

    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { StatusCode: null or StatusCodes.Status200OK, Value: { } value })
        {
            var options = context.HttpContext.RequestServices
                .GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

            // El ETag se calcula sobre los mismos bytes que se envían.
            var body = JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), options);

            context.HttpContext.Response.Headers.CacheControl = CacheControl;
            context.Result = new FileContentResult(body, ContentType)
            {
                EntityTag = new EntityTagHeaderValue($"\"{Convert.ToHexStringLower(SHA256.HashData(body))}\""),
            };
        }

        return next();
    }
}
