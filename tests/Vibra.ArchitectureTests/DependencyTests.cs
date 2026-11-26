using Vibra.ArchitectureTests.Rules;

namespace Vibra.ArchitectureTests;

public class DependencyTests
{
    public static TheoryData<string> ModuleNames => [.. Modules.Assemblies.Keys];

    [Fact]
    public void Every_module_folder_is_covered()
    {
        var folders = Directory.GetDirectories(Path.Combine(RepositoryPaths.Root, "src", "Modules"))
            .Select(Path.GetFileName)
            .Order();

        Assert.Equal(Modules.Assemblies.Keys.Order(), folders);
        Assert.Equal(6,Modules.Assemblies.Count);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Every_module_has_the_four_layers(string module)
    {
        var namespaces = Modules.Assemblies[module].GetTypes().Select(type => type.Namespace).ToList();

        Assert.All(Modules.Layers, layer =>
            Assert.Contains($"{Modules.RootNamespace}.{module}.{layer}", namespaces));
    }

    // Sin esto, un tipo fuera del espacio de nombres del módulo escaparía de las reglas.
    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Every_type_lives_in_a_layer_namespace(string module)
    {
        var prefixes = Modules.Layers.Select(layer => $"{Modules.RootNamespace}.{module}.{layer}").ToList();

        var strays = Modules.Assemblies[module].GetTypes()
            .Where(type => type.Namespace?.StartsWith("Vibra", StringComparison.Ordinal) ?? !type.Name.StartsWith('<'))
            .Where(type => !prefixes.Any(prefix =>
                type.Namespace == prefix || (type.Namespace?.StartsWith(prefix + ".", StringComparison.Ordinal) ?? false)))
            .Select(type => type.FullName);

        Assert.Empty(strays);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_does_not_use_another_modules_infrastructure(string module) =>
        Assert.Empty(DependencyRules.ForeignInfrastructure(
            Modules.Assemblies[module], Modules.RootNamespace, module, Modules.Assemblies.Keys));

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_source_does_not_name_another_modules_infrastructure(string module)
    {
        var folder = Path.Combine(RepositoryPaths.Root, "src", "Modules", module) + Path.DirectorySeparatorChar;
        var files = RepositoryPaths.SourceFiles()
            .Where(file => file.StartsWith(folder, StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(files);
        Assert.Empty(files.SelectMany(file => SourceRules.ForeignInfrastructureNames(
            File.ReadAllText(file), module, Modules.Assemblies.Keys)));
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Domain_does_not_depend_on_outer_layers(string module) =>
        Assert.Empty(DependencyRules.ForbiddenLayers(
            Modules.Assemblies[module], $"{Modules.RootNamespace}.{module}",
            "Domain", "Application", "Infrastructure", "Api"));

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Api_does_not_depend_on_infrastructure(string module)
    {
        // La clase que registra el módulo es la única que deberá conectar Infrastructure.
        var violations = DependencyRules.ForbiddenLayers(
                Modules.Assemblies[module], $"{Modules.RootNamespace}.{module}", "Api", "Infrastructure")
            .Where(type => type != $"{Modules.RootNamespace}.{module}.Api.{module}Module");

        Assert.Empty(violations);
    }
}
