using MarkdownDocumentConverter.Core.Formatting;
using MarkdownDocumentConverter.Core.Infrastructure;
using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Tests.Infrastructure;

namespace MarkdownDocumentConverter.Tests.Unit;

public sealed class TypstTemplateRendererTests
{
    [Fact]
    public void Replaces_all_placeholders_and_applies_request_values()
    {
        using var output = new TestOutputDirectory();
        var locator = new ResourceLocator();
        var renderer = new TypstTemplateRenderer();
        var style = StyleCatalog.GetById("academic");
        var request = new ConversionRequest
        {
            InputPath = TestPaths.Fixture("basic.md"),
            OutputDirectory = output.Path,
            PageSize = PageSize.A5,
            FontFamily = "Arial",
            FontScale = 1.1,
        };
        var destination = output.File("template.typ");

        renderer.Render(locator.GetTypstTemplatePath("academic"), destination, style, request);

        var template = File.ReadAllText(destination);
        Assert.DoesNotContain("@@", template, StringComparison.Ordinal);
        Assert.Contains("paper: \"a5\"", template, StringComparison.Ordinal);
        Assert.Contains("font: (\"Arial\"", template, StringComparison.Ordinal);
        Assert.Contains("footer: context", template, StringComparison.Ordinal);
        Assert.Contains("size: 13.2pt", template, StringComparison.Ordinal);
    }
}
