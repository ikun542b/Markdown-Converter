namespace MarkdownDocumentConverter.Core.Models;

public sealed record PageMargins(
    double TopMm,
    double RightMm,
    double BottomMm,
    double LeftMm)
{
    public static PageMargins Default { get; } = new(25, 24, 25, 24);
}
