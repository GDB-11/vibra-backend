using System.Reflection;
using Vibra.Modules.Billing.Domain;
using Vibra.Modules.Catalog.Domain;
using Vibra.Modules.History.Domain;
using Vibra.Modules.Identity.Domain;
using Vibra.Modules.Library.Domain;
using Vibra.Modules.Search.Domain;
using Vibra.Modules.Streaming.Domain;

namespace Vibra.ArchitectureTests;

internal static class Modules
{
    public const string RootNamespace = "Vibra.Modules";

    public static readonly string[] Layers = ["Domain", "Application", "Infrastructure", "Api"];

    public static readonly IReadOnlyDictionary<string, Assembly> Assemblies = new Dictionary<string, Assembly>
    {
        ["Identity"] = typeof(IdentityDomainMarker).Assembly,
        ["Catalog"] = typeof(CatalogDomainMarker).Assembly,
        ["Library"] = typeof(LibraryDomainMarker).Assembly,
        ["Streaming"] = typeof(StreamingDomainMarker).Assembly,
        ["History"] = typeof(HistoryDomainMarker).Assembly,
        ["Search"] = typeof(SearchDomainMarker).Assembly,
        ["Billing"] = typeof(BillingDomainMarker).Assembly,
    };
}
