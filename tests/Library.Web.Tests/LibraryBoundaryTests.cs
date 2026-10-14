using Library.Web.Plugin;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Providers;
using AethericForge.Runtime.Models.Authorities;
using AethericForge.Runtime.Models.Library.Articles;
using AethericForge.Runtime.Providers.Knowledge.InMemory;
using AethericForge.Runtime.Services.Knowledge;
using AethericForge.Runtime.Services.Library;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public sealed class LibraryBoundaryTests
{
    [Fact]
    public async Task Existing_article_is_read_and_revised_without_changing_its_storage_identity()
    {
        var storage = new InMemoryKnowledgeProvider("ForgeCampus");
        var knowledge = new KnowledgeService([storage], new Team<AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Services.ICuratorClerk>([]));
        var legacy = new ArticleLibrary(knowledge, new ArticleLibraryOptions { KnowledgeScheme = "ForgeCampus", Authority = new() { Id = "forge-library", Name = "Library" } });
        var article = await legacy.CreateAsync(new Article { Identity = new() { Title = "Before extraction", Slug = "before-extraction" }, Content = new() { Body = "Existing text", BodyFormat = ArticleContentFormats.Markdown }, Provenance = new() { Authority = new() { Id = "forge-library", Name = "Library" }, CreatedBy = new() { DisplayName = "Author" }, UpdatedBy = new() { DisplayName = "Author" } } });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["Library:MongoDb:Host"]="unused.example", ["Library:MongoDb:DatabaseName"]="library",
            ["Library:MongoDb:Username"]="library", ["Library:MongoDb:Password"]="test-only"
        }).Build();
        var services = new ServiceCollection();
        new LibraryWebFactory().ConfigureWebServices(services, configuration, _ => null);
        services.RemoveAllKeyed<IKnowledgeProvider>("Library");
        services.AddKeyedSingleton<IKnowledgeProvider>("Library", storage);
        using var provider = services.BuildServiceProvider();
        var module = provider.GetRequiredService<IArticleLibrary>();
        var read = await module.GetAsync(article.Identity.CanonicalId);
        Assert.Equal("Existing text", read!.Content.Body);
        var saved = await module.SaveAsync(read with { Content = read.Content with { Body = "Revised text" } });
        Assert.Equal(article.Identity.CanonicalId, saved.Identity.CanonicalId);
        Assert.Equal(article.Publication.Revision + 1, saved.Publication.Revision);
        Assert.Equal("Revised text", (await legacy.GetAsync(article.Identity.CanonicalId))!.Content.Body);
    }

    [Fact]
    public void Library_never_inherits_platform_credentials()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        { ["MongoDb:Username"]="platform", ["MongoDb:Password"]="platform" }).Build();
        Assert.Throws<InvalidOperationException>(() => new LibraryWebFactory().ConfigureWebServices(new ServiceCollection(), configuration, _ => null));
    }
}
