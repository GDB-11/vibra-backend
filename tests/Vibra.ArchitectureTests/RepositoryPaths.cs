namespace Vibra.ArchitectureTests;

internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Vibra.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("No se encontró Vibra.slnx.");
    }

    /// <summary>Código fuente de <c>src/</c>, sin las carpetas de compilación.</summary>
    public static IReadOnlyList<string> SourceFiles() =>
    [
        .. Directory.EnumerateFiles(Path.Combine(Root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(Root, path)
                .Split(Path.DirectorySeparatorChar)
                .Any(part => part is "bin" or "obj")),
    ];
}
