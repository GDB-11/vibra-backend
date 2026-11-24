using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Vibra.ArchitectureTests.Rules;

/// <summary>Reglas sobre el código fuente que NetArchTest no puede comprobar.</summary>
internal static class SourceRules
{
    private static readonly string[] ExecuteMethods = ["ExecuteAsync", "ExecuteAuthenticatedAsync"];

    private static readonly string[] MinimalApiMethods =
        ["MapGet", "MapPost", "MapPut", "MapDelete", "MapPatch", "MapMethods", "MapGroup", "MapFallback"];

    /// <summary>Cada método de un controlador es una sola llamada a <c>Execute*Async</c>, sin lógica.</summary>
    public static IReadOnlyList<string> ThickControllers(string source) =>
    [
        .. Parse(source).DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(type => type.Identifier.Text.EndsWith("Controller", StringComparison.Ordinal)
                           && !type.Modifiers.Any(SyntaxKind.AbstractKeyword))
            .SelectMany(type => type.Members.OfType<MethodDeclarationSyntax>()
                .Where(method => !IsThin(method))
                .Select(method => $"{type.Identifier.Text}.{method.Identifier.Text}")),
    ];

    public static IReadOnlyList<string> MinimalApis(string source) =>
    [
        .. Parse(source).DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsMinimalApi)
            .Select(invocation => invocation.ToString()),
    ];

    public static IReadOnlyList<string> SystemClock(string source) =>
    [
        .. Parse(source).DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(access => access.Name.Identifier.Text is "Now" or "UtcNow" or "Today"
                             && access.Expression.ToString().Split('.', ':')[^1] is "DateTime" or "DateTimeOffset")
            .Select(access => access.ToString()),
    ];

    /// <summary>
    /// Menciones a la infraestructura de otros módulos. Complementa a NetArchTest, que no ve las
    /// constantes (como el SQL) porque el compilador copia su valor en quien las usa.
    /// </summary>
    public static IReadOnlyList<string> ForeignInfrastructureNames(
        string source, string module, IEnumerable<string> allModules)
    {
        var foreign = allModules.Where(other => other != module)
            .Select(other => $".{other}.Infrastructure.")
            .ToList();

        return
        [
            .. Parse(source).DescendantNodes()
                .Where(node => node is QualifiedNameSyntax or MemberAccessExpressionSyntax)
                .Select(node => string.Concat(node.DescendantTokens().Select(token => token.Text)))
                .Where(name => foreign.Any(prefix =>
                    $".{name.Replace("global::", string.Empty)}.".Contains(prefix, StringComparison.Ordinal)))
                .Distinct(),
        ];
    }

    private static SyntaxNode Parse(string source) => CSharpSyntaxTree.ParseText(source).GetRoot();

    private static bool IsThin(MethodDeclarationSyntax method) =>
        method.ExpressionBody?.Expression is InvocationExpressionSyntax invocation
        && invocation.Expression is SimpleNameSyntax name
        && ExecuteMethods.Contains(name.Identifier.Text)
        && !invocation.DescendantNodes().Any(node =>
            node is BlockSyntax or ConditionalExpressionSyntax or SwitchExpressionSyntax);

    private static bool IsMinimalApi(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax access)
            return false;

        var method = access.Name.Identifier.Text;

        // "Map" también es el operador de BindSharp: solo es una ruta si empieza con "/".
        return MinimalApiMethods.Contains(method)
               || (method == "Map"
                   && invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax literal
                   && literal.Token.ValueText.StartsWith('/'));
    }
}
