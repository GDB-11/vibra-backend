using Vibra.Api;

namespace Vibra.Migrations.Tests;

public class MigrationOrderTests
{
    [Fact]
    public void Modules_run_in_the_order_required_by_cross_module_keys() =>
        Assert.Equal(
            ["identity", "catalog", "streaming", "library", "history", "billing"],
            VibraMigrations.All.Select(module => module.Name));

    [Fact]
    public void Every_module_folder_has_its_migrations_registered()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);

        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Vibra.slnx")))
            root = root.Parent;

        Assert.NotNull(root);

        var folders = Directory.GetDirectories(Path.Combine(root.FullName, "src", "Modules"))
            .Select(folder => Path.GetFileName(folder).ToLowerInvariant());

        Assert.Equal(folders.Order(), VibraMigrations.All.Select(module => module.Name).Order());
    }
}
