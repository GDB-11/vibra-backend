using Vibra.ArchitectureTests.Fixtures.Controllers;
using Vibra.ArchitectureTests.Rules;

namespace Vibra.ArchitectureTests;

/// <summary>Comprueban que cada regla detecta una violación real y no pasa en vacío.</summary>
public class NegativeControlTests
{
    private const string FixtureRoot = "Vibra.ArchitectureTests.Fixtures.Modules";

    [Fact]
    public void Foreign_infrastructure_is_detected()
    {
        var violations = DependencyRules.ForeignInfrastructure(
            typeof(NegativeControlTests).Assembly, FixtureRoot, "Beta", ["Alpha", "Beta"]);

        Assert.Equal(
            ["UsesInAsyncMethod", "UsesInConstructor", "UsesInMethodBody"],
            violations.Select(name => name.Split('.')[^1].Split('/')[0]).Distinct().Order());
    }

    [Theory]
    [InlineData("using Vibra.Modules.Catalog.Infrastructure;")]
    [InlineData("using Sql = Vibra.Modules.Catalog.Infrastructure.TrackSql;")]
    [InlineData("class A { string B = Vibra.Modules.Catalog.Infrastructure.TrackSql.Select; }")]
    [InlineData("class A { string B = global::Vibra.Modules.Catalog.Infrastructure.TrackSql.Select; }")]
    [InlineData("class A { string B = Catalog.Infrastructure.TrackSql.Select; }")]
    public void Foreign_infrastructure_name_is_detected(string source) =>
        Assert.NotEmpty(SourceRules.ForeignInfrastructureNames(source, "Billing", ["Billing", "Catalog"]));

    [Fact]
    public void Own_infrastructure_name_is_allowed() =>
        Assert.Empty(SourceRules.ForeignInfrastructureNames(
            "using Vibra.Modules.Billing.Infrastructure; using Vibra.Modules.Catalog.Application;",
            "Billing", ["Billing", "Catalog"]));

    [Fact]
    public void Forbidden_layer_is_detected()
    {
        var violations = DependencyRules.ForbiddenLayers(
            typeof(NegativeControlTests).Assembly, $"{FixtureRoot}.Beta", "Domain", "Application");

        Assert.Contains(violations, name => name.EndsWith("DomainUsingApplication", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(typeof(PlainController), "no deriva de FunctionalController")]
    [InlineData(typeof(RepeatedAttributeController), "[ApiController]")]
    [InlineData(typeof(DataAccessController), "acceso a datos")]
    public void Controller_violation_is_detected(Type controller, string expected) =>
        Assert.Contains(expected, Assert.Single(ControllerRules.Violations([controller])));

    [Fact]
    public void Valid_controller_passes() => Assert.Empty(ControllerRules.Violations([typeof(ValidController)]));

    [Theory]
    [InlineData("public Task<IActionResult> Get() { return ExecuteAsync(() => s.Get(), m, \"get\"); }")]
    [InlineData("public IActionResult Get() => Ok();")]
    [InlineData("public Task<IActionResult> Get(int id) => id > 0 ? ExecuteAsync(a, m, \"a\") : ExecuteAsync(b, m, \"b\");")]
    [InlineData("public Task<IActionResult> Get() => ExecuteAsync(async () => { return await s.Get(); }, m, \"get\");")]
    [InlineData("public Task<IActionResult> Get(int id) => ExecuteAsync(() => id > 0 ? s.A() : s.B(), m, \"get\");")]
    [InlineData("private int Helper() { return 1; }")]
    public void Thick_action_is_detected(string method) =>
        Assert.Single(SourceRules.ThickControllers($"public sealed class TracksController {{ {method} }}"));

    [Theory]
    [InlineData("public Task<IActionResult> Get(Guid id, CancellationToken ct) => ExecuteAsync(() => s.GetAsync(id, ct), m, \"get\");")]
    [InlineData("public Task<IActionResult> Me(CancellationToken ct) => ExecuteAuthenticatedAsync(user => s.GetAsync(user, ct), m, \"me\");")]
    public void Thin_action_passes(string method) =>
        Assert.Empty(SourceRules.ThickControllers($"public sealed class TracksController {{ {method} }}"));

    [Theory]
    [InlineData("app.MapGet(\"/a\", () => 1);")]
    [InlineData("app.MapPost(\"/a\", () => 1);")]
    [InlineData("app.MapGroup(\"/v1\");")]
    [InlineData("app.Map(\"/a\", () => 1);")]
    public void Minimal_api_is_detected(string statement) => Assert.Single(SourceRules.MinimalApis(statement));

    [Fact]
    public void Result_map_and_controller_mapping_are_allowed() =>
        Assert.Empty(SourceRules.MinimalApis("app.MapControllers(); result.Map(x => x);"));

    [Theory]
    [InlineData("var a = DateTime.UtcNow;")]
    [InlineData("var a = DateTimeOffset.UtcNow;")]
    [InlineData("var a = System.DateTime.Now;")]
    [InlineData("var a = global::System.DateTimeOffset.Now;")]
    [InlineData("var a = DateTime.Today;")]
    public void System_clock_is_detected(string statement) => Assert.Single(SourceRules.SystemClock(statement));

    [Fact]
    public void Injected_clock_is_allowed() =>
        Assert.Empty(SourceRules.SystemClock("var a = timeProvider.GetUtcNow();"));
}
