using MarkdownDocumentConverter.Core.Exceptions;
using MarkdownDocumentConverter.Core.Infrastructure;

namespace MarkdownDocumentConverter.Tests.Unit;

public sealed class ResourceLocatorTests
{
    [Fact]
    public void Finds_vendored_engines()
    {
        var locator = new ResourceLocator();

        Assert.NotNull(locator.ResourceRoot);
        Assert.True(locator.TryGetEnginePath("pandoc.exe", out var pandocPath));
        Assert.True(locator.TryGetEnginePath("typst.exe", out var typstPath));
        Assert.True(File.Exists(pandocPath));
        Assert.True(File.Exists(typstPath));
    }

    [Fact]
    public void Missing_engine_throws_resource_not_found()
    {
        var locator = new ResourceLocator();

        Assert.Throws<ResourceNotFoundException>(() => locator.GetEnginePath("missing-engine.exe"));
    }
}
