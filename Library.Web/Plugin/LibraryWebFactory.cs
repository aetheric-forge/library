using AethericForge.Runtime.Institutions.Abstractions.Models;
using System.Runtime.Loader;
using AethericForge.Runtime.Abstractions.Interfaces.Institutions;
using AethericForge.Runtime.Abstractions.Interfaces.Institutions.Plugins;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Providers;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Institutions.Abstractions.Builders;
using AethericForge.Runtime.Institutions.Abstractions.Primitives;
using AethericForge.Runtime.Institutions.Library;
using AethericForge.Runtime.Models.Authorities;
using AethericForge.Runtime.Models.Library.Articles;
using AethericForge.Runtime.Providers.Knowledge.MongoDb;
using AethericForge.Runtime.Services.Knowledge;
using AethericForge.Runtime.Services.Library;
using Forge.Primitives.MongoDb;
using Library.Web.Authentication;
using Library.Web.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

[assembly: InstitutionPluginPackage(typeof(Library.Web.Plugin.LibraryWebPackage))]
namespace Library.Web.Plugin;

public sealed class LibraryWebPackage : IInstitutionPluginPackage
{
    public IReadOnlyCollection<IInstitutionFactory> GetFactories() => [new LibraryWebFactory()];
}

public sealed class LibraryWebFactory : IInstitutionFactory
{
    private readonly AssemblyLoadContext _context = AssemblyLoadContext.GetLoadContext(typeof(LibraryWebFactory).Assembly)!;
    public Type ContractType => typeof(ILibrary);
    public IInstitutionTemplate Template { get; } = InstitutionTemplateBuilder.Create()
        .WithDescriptor("Library", new Version(1, 0, 0), "Canonical articles and durable knowledge.")
        .AddResource(new ResourceDefinition("Library storage", "knowledge", "Canonical articles and durable knowledge.")).Build();
    public IInstitutionManifest Manifest => Template.Descriptor;

    public void ConfigureWebServices(IServiceCollection services, IConfiguration configuration,
        Func<IServiceProvider, IInstitution?> parent)
    {
        MongoBsonSetup.EnsureGuidRepresentationRegistered();
        var options = InstitutionServiceConfiguration.Resolve(configuration, "Library", "MongoDb")
            .GetSection("MongoDb").Get<MongoOptions>()!;
        if (string.IsNullOrWhiteSpace(options.Host) || string.IsNullOrWhiteSpace(options.DatabaseName))
            throw new InvalidOperationException("Library MongoDb Host and DatabaseName are required.");
        services.AddKeyedSingleton<IMongoClient>("Library", (_, _) => new MongoClient(options.ToConnectionString()));
        services.AddKeyedSingleton<IMongoDatabase>("Library", (sp, _) => sp.GetRequiredKeyedService<IMongoClient>("Library").GetDatabase(options.DatabaseName));
        services.AddKeyedSingleton<IKnowledgeProvider>("Library", (sp, _) =>
            new MongoDbKnowledgeProvider(sp.GetRequiredKeyedService<IMongoDatabase>("Library"), "ForgeCampus", "knowledge"));
        services.AddKeyedSingleton<IKnowledgeService>("Library", (sp, _) =>
            new KnowledgeService(sp.GetKeyedServices<IKnowledgeProvider>("Library"), new Team<ICuratorClerk>([])));
        services.AddKeyedSingleton<ILibrary>("Library", (sp, _) => parent(sp) is { } owner
            ? owner.Resolve<ILibrary>() : CreateLibrary(sp, null));
        services.AddSingleton<IArticleLibrary>(sp => new ArticleLibrary(sp.GetRequiredKeyedService<IKnowledgeService>("Library"), new ArticleLibraryOptions
        {
            KnowledgeScheme = "ForgeCampus",
            Authority = new InstitutionReference { Id = "forge-library", Name = "Library" },
            DefaultOrigin = new InstitutionReference { Id = "forge-campus", Name = "Forge Campus" }
        }));
        services.AddScoped<ICurrentIdentityAccessor, CurrentIdentityAccessor>();
    }

    public IInstitution Create(IInstitution parent, IServiceProvider services) => CreateLibrary(services, parent);
    private ILibrary CreateLibrary(IServiceProvider services, IInstitution? parent)
    {
        var knowledge = services.GetRequiredKeyedService<IKnowledgeService>("Library");
        return new AethericForge.Runtime.Institutions.Library.Library(new LibraryContext(Template, new OwnedServices(services, knowledge), parent),
            new Librarian(knowledge, new Team<ILibraryClerk>([])), new Curator(knowledge, new Team<ICuratorClerk>([])));
    }
    private sealed class OwnedServices(IServiceProvider host, IKnowledgeService knowledge) : IServiceProvider
    {
        public object? GetService(Type type) => type == typeof(IKnowledgeService) ? knowledge : host.GetService(type);
    }

}
