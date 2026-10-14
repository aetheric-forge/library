using System.Security.Cryptography;
using System.Text;
using Aetheric.Provisioning.Definitions;
using Xunit;

public sealed class DefinitionTests
{
    [Fact]
    public void Definition_and_bindings_load_through_the_runtime_Git_document_reader()
    {
        SourceDocument Document(string file)
        {
            var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "institution", file));
            return new(text, new("https://github.com/aetheric-forge/library", new string('a',40), file,
                Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)))));
        }
        var result = new InstitutionYamlReader().Read(new(Document("library.yaml"), Document("library.bindings.yaml")));
        Assert.Empty(result.Issues);
        Assert.Equal("library", result.Institution!.Requirements.Id);
        Assert.Single(result.Institution.Requirements.Resources);
    }
}
