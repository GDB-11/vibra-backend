using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Vibra.Shared.Api;

namespace Vibra.Shared.Tests.Http;

public sealed class RequestValidationTests : IClassFixture<ProbeApiFactory>
{
    private readonly ProbeApiFactory _factory;

    public RequestValidationTests(ProbeApiFactory factory)
    {
        _factory = factory;
    }

    // En ejecución, un cuerpo sin validador responde 500. Esta prueba lo detecta antes.
    [Fact]
    public void Every_request_body_has_a_validator()
    {
        var bodies = _factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>()
            .ActionDescriptors.Items
            .SelectMany(action => action.Parameters)
            .Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(parameter => parameter.ParameterType)
            .Distinct()
            .ToList();

        Assert.NotEmpty(bodies);
        Assert.DoesNotContain(bodies, body =>
            _factory.Services.GetService(typeof(IRequestValidator<>).MakeGenericType(body)) is null);
    }
}
