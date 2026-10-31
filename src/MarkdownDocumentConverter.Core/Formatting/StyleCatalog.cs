using MarkdownDocumentConverter.Core.Exceptions;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Formatting;

public static class StyleCatalog
{
    public static DocumentStyle Modern { get; } = new(
        Id: "modern",
        Name: "现代文档",
        Description: "清晰的标题层级、适度段间距，适合日常文档与项目说明。",
        BaseFontSizePt: 11,
        FontFamily: null,
        LineSpacing: 1.5,
        ParagraphSpacingPt: 6,
        HeadingScale: 1.25,
        PageMargins: new PageMargins(25, 24, 25, 24),
        HeaderEnabled: true,
        FooterEnabled: true,
        PageNumberEnabled: true,
        TocEnabled: true,
        CodeStyle: "modern",
        TableStyle: "clean");

    public static DocumentStyle Academic { get; } = new(
        Id: "academic",
        Name: "学术",
        Description: "严格的层级与规范的页眉页脚，适合论文、研究报告和长文档。",
        BaseFontSizePt: 12,
        FontFamily: null,
        LineSpacing: 1.5,
        ParagraphSpacingPt: 6,
        HeadingScale: 1.2,
        PageMargins: new PageMargins(30, 28, 30, 28),
        HeaderEnabled: true,
        FooterEnabled: true,
        PageNumberEnabled: true,
        TocEnabled: true,
        CodeStyle: "academic",
        TableStyle: "bordered");

    public static DocumentStyle Minimal { get; } = new(
        Id: "minimal",
        Name: "简约",
        Description: "装饰少、版面紧凑，适合打印和简单说明文档。",
        BaseFontSizePt: 10.5,
        FontFamily: null,
        LineSpacing: 1.3,
        ParagraphSpacingPt: 4,
        HeadingScale: 1.2,
        PageMargins: new PageMargins(20, 20, 20, 20),
        HeaderEnabled: false,
        FooterEnabled: true,
        PageNumberEnabled: true,
        TocEnabled: false,
        CodeStyle: "minimal",
        TableStyle: "minimal");

    public static IReadOnlyList<DocumentStyle> All { get; } =
        new[] { Modern, Academic, Minimal };

    public static DocumentStyle? FindById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return All.FirstOrDefault(
            style => string.Equals(style.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static DocumentStyle GetById(string id)
    {
        return FindById(id)
            ?? throw new ConversionException($"未知的文档样式：{id}");
    }
}
