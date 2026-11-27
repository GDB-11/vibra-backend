using Vibra.ArchitectureTests.Rules;

namespace Vibra.ArchitectureTests;

public class ConventionTests
{
    [Fact]
    public void Controllers_follow_the_shared_base() =>
        Assert.Empty(ControllerRules.Violations(
            Modules.Assemblies.Values.Append(typeof(Program).Assembly).SelectMany(assembly => assembly.GetTypes())));

    [Fact]
    public void Controller_actions_are_a_single_expression() => AssertSources(SourceRules.ThickControllers);

    [Fact]
    public void Minimal_apis_are_not_used() => AssertSources(SourceRules.MinimalApis);

    [Fact]
    public void System_clock_is_not_used() => AssertSources(SourceRules.SystemClock);

    [Fact]
    public void Primary_constructors_are_not_used() =>
        AssertFiles([.. RepositoryPaths.SourceFiles(), .. RepositoryPaths.TestFiles()], SourceRules.PrimaryConstructors);

    private static void AssertSources(Func<string, IReadOnlyList<string>> rule) =>
        AssertFiles(RepositoryPaths.SourceFiles(), rule);

    private static void AssertFiles(IReadOnlyList<string> files, Func<string, IReadOnlyList<string>> rule)
    {
        Assert.NotEmpty(files);
        Assert.Empty(files.SelectMany(file => rule(File.ReadAllText(file))
            .Select(violation => $"{Path.GetRelativePath(RepositoryPaths.Root, file)}: {violation}")));
    }
}
