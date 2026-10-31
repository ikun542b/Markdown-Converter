namespace MarkdownDocumentConverter.Core.Models;

public sealed record DocumentStyle(
    string Id,
    string Name,
    string Description,
    double BaseFontSizePt,
    string? FontFamily,
    double LineSpacing,
    double ParagraphSpacingPt,
    double HeadingScale,
    PageMargins PageMargins,
    bool HeaderEnabled,
    bool FooterEnabled,
    bool PageNumberEnabled,
    bool TocEnabled,
    string CodeStyle,
    string TableStyle);
