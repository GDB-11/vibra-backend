using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Vibra.Shared.Api;

namespace Vibra.ArchitectureTests.Rules;

internal static class ControllerRules
{
    private static readonly string[] DataAccessNamespaces = ["Dapper", "Npgsql", "System.Data"];

    public static IReadOnlyList<string> Violations(IEnumerable<Type> types) =>
        [.. types.Where(IsController).SelectMany(Check)];

    public static bool IsController(Type type) =>
        type is { IsClass: true, IsAbstract: false }
        && (type.IsAssignableTo(typeof(ControllerBase))
            || type.Name.EndsWith("Controller", StringComparison.Ordinal));

    private static IEnumerable<string> Check(Type controller)
    {
        if (!controller.IsSubclassOf(typeof(FunctionalController)))
            yield return $"{controller.Name}: no deriva de FunctionalController";

        for (var type = controller; type is not null && type != typeof(FunctionalController); type = type.BaseType)
        {
            if (type.GetCustomAttribute<ApiControllerAttribute>(inherit: false) is not null)
                yield return $"{controller.Name}: [ApiController] solo va en FunctionalController";
        }

        var dependencies = controller.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .Where(IsDataAccess);

        foreach (var dependency in dependencies)
            yield return $"{controller.Name}: depende del acceso a datos ({dependency.Name})";
    }

    private static bool IsDataAccess(Type type)
    {
        var name = type.Namespace ?? string.Empty;

        return DataAccessNamespaces.Any(prefix => name == prefix || name.StartsWith(prefix + ".", StringComparison.Ordinal))
            || name.Split('.').Contains("Infrastructure");
    }
}
