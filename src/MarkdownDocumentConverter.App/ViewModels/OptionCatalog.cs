using MarkdownDocumentConverter.Core.Formatting;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.App.ViewModels;

public static class OptionCatalog
{
    public static IReadOnlyList<OptionItem<PageSize>> PageSizes { get; } = new[]
    {
        new OptionItem<PageSize>("A4", PageSize.A4),
        new OptionItem<PageSize>("A5", PageSize.A5),
    };

    public static IReadOnlyList<OptionItem<string>> Styles { get; } = StyleCatalog.All
        .Select(style => new OptionItem<string>(style.Name, style.Id))
        .ToArray();

    public static IReadOnlyList<OptionItem<double>> FontScales { get; } = new[]
    {
        new OptionItem<double>("较小（85%）", 0.85),
        new OptionItem<double>("小（92%）", 0.92),
        new OptionItem<double>("标准（100%）", 1.0),
        new OptionItem<double>("大（110%）", 1.1),
        new OptionItem<double>("较大（120%）", 1.2),
    };

    public static IReadOnlyList<OptionItem<OverwritePolicy>> OverwritePolicies { get; } = new[]
    {
        new OptionItem<OverwritePolicy>("自动添加编号", OverwritePolicy.Rename),
        new OptionItem<OverwritePolicy>("覆盖已有文件", OverwritePolicy.Overwrite),
        new OptionItem<OverwritePolicy>("跳过已有文件", OverwritePolicy.Skip),
    };

    public static IReadOnlyList<OptionItem<string?>> BuildFontOptions(IEnumerable<string> fonts)
    {
        var options = new List<OptionItem<string?>>
        {
            new("默认字体", null),
        };

        options.AddRange(
            fonts.Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(font => font, StringComparer.CurrentCultureIgnoreCase)
                .Select(font => new OptionItem<string?>(font, font)));

        return options;
    }
}
