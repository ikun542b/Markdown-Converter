using MarkdownDocumentConverter.Core.Exceptions;
using MarkdownDocumentConverter.Core.Formatting;

namespace MarkdownDocumentConverter.Tests.Unit;

public sealed class StyleCatalogTests
{
    [Fact]
    public void Catalog_has_three_distinct_styles_with_valid_metrics()
    {
        Assert.Equal(3, StyleCatalog.All.Count);
        Assert.Equal(3, StyleCatalog.All.Select(style => style.Id).Distinct().Count());

        Assert.All(StyleCatalog.All, style =>
        {
            Assert.True(style.BaseFontSizePt > 0);
            Assert.True(style.LineSpacing > 0);
            Assert.True(style.HeadingScale > 0);
            Assert.True(style.PageMargins.TopMm > 0);
            Assert.True(style.PageMargins.RightMm > 0);
            Assert.True(style.PageMargins.BottomMm > 0);
            Assert.True(style.PageMargins.LeftMm > 0);
        });
    }

    [Fact]
    public void Lookup_is_case_insensitive()
    {
        var style = StyleCatalog.FindById("ACADEMIC");

        Assert.NotNull(style);
        Assert.Equal("academic", style.Id);
    }

    [Fact]
    public void Unknown_style_throws_clear_error()
    {
        var exception = Assert.Throws<ConversionException>(() => StyleCatalog.GetById("missing"));

        Assert.Contains("missing", exception.Message, StringComparison.Ordinal);
    }
}
