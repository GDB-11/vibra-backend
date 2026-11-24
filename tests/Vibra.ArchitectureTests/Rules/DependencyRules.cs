using System.Reflection;
using NetArchTest.Rules;

namespace Vibra.ArchitectureTests.Rules;

internal static class DependencyRules
{
    /// <summary>Tipos de <paramref name="module"/> que usan la infraestructura de otro módulo.</summary>
    public static IReadOnlyList<string> ForeignInfrastructure(
        Assembly assembly, string rootNamespace, string module, IEnumerable<string> allModules) =>
        FailingTypes(
            assembly,
            $"{rootNamespace}.{module}",
            [.. allModules.Where(other => other != module).Select(other => $"{rootNamespace}.{other}.Infrastructure")]);

    /// <summary>Tipos de una capa que dependen de capas prohibidas de su mismo módulo.</summary>
    public static IReadOnlyList<string> ForbiddenLayers(
        Assembly assembly, string moduleNamespace, string layer, params string[] forbidden) =>
        FailingTypes(
            assembly,
            $"{moduleNamespace}.{layer}",
            [.. forbidden.Select(other => $"{moduleNamespace}.{other}")]);

    private static IReadOnlyList<string> FailingTypes(Assembly assembly, string sourceNamespace, string[] forbidden)
    {
        var result = Types.InAssembly(assembly)
            .That().ResideInNamespace(sourceNamespace)
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult();

        return result.IsSuccessful ? [] : [.. result.FailingTypeNames];
    }
}
