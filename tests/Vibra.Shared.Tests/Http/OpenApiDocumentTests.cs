using System.Text.Json;

namespace Vibra.Shared.Tests.Http;

public sealed class OpenApiDocumentTests : IClassFixture<ProbeApiFactory>
{
    private readonly ProbeApiFactory _factory;

    public OpenApiDocumentTests(ProbeApiFactory factory)
    {
        _factory = factory;
    }

    // El cliente Swift se genera de este documento: un entero no puede ser también un texto.
    [Fact]
    public async Task Numbers_have_a_single_type()
    {
        using var client = _factory.CreateClient();

        var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json")).RootElement;
        var types = TypeLists(document).ToList();

        Assert.NotEmpty(types);
        Assert.DoesNotContain(
            types, list => list.Contains("string") && (list.Contains("integer") || list.Contains("number")));
    }

    private static IEnumerable<List<string?>> TypeLists(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
            return element.EnumerateArray().SelectMany(TypeLists);

        if (element.ValueKind != JsonValueKind.Object)
            return [];

        return element.EnumerateObject().SelectMany(property =>
            property is { Name: "type", Value.ValueKind: JsonValueKind.Array }
                ? [property.Value.EnumerateArray().Select(item => item.GetString()).ToList()]
                : TypeLists(property.Value));
    }
}
