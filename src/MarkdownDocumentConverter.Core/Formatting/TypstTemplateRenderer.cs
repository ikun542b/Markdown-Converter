using System.Globalization;
using System.Text;
using MarkdownDocumentConverter.Core.Exceptions;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Formatting;

public sealed class TypstTemplateRenderer
{
    public void Render(
        string templatePath,
        string outputPath,
        DocumentStyle style,
        ConversionRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(style);
        ArgumentNullException.ThrowIfNull(request);

        if (!File.Exists(templatePath))
        {
            throw new ConversionException($"找不到 Typst 模板：{templatePath}");
        }

        var scale = Math.Clamp(request.FontScale, 0.5, 2.0);
        var fontSize = style.BaseFontSizePt * scale;
        var fontFamily = string.IsNullOrWhiteSpace(request.FontFamily)
            ? DefaultFontFamily(style.Id)
            : request.FontFamily.Trim();

        var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["@@PAPER@@"] = request.PageSize == PageSize.A5 ? "a5" : "a4",
            ["@@MARGIN_TOP@@"] = FormatNumber(style.PageMargins.TopMm),
            ["@@MARGIN_RIGHT@@"] = FormatNumber(style.PageMargins.RightMm),
            ["@@MARGIN_BOTTOM@@"] = FormatNumber(style.PageMargins.BottomMm),
            ["@@MARGIN_LEFT@@"] = FormatNumber(style.PageMargins.LeftMm),
            ["@@FONT_LIST@@"] = BuildFontList(fontFamily, style.Id),
            ["@@MONO_FONT_LIST@@"] = "\"Consolas\", \"Cascadia Mono\", \"DejaVu Sans Mono\"",
            ["@@FONT_SIZE@@"] = FormatNumber(fontSize),
            ["@@LEADING@@"] = FormatNumber(style.LineSpacing * 0.65),
            ["@@PAR_SPACING@@"] = FormatNumber(style.ParagraphSpacingPt * scale),
            ["@@TITLE_SIZE@@"] = FormatNumber(TitleSize(style.Id)),
            ["@@HEADING_BEFORE@@"] = FormatNumber(HeadingSpacing(style.Id).Before),
            ["@@HEADING_AFTER@@"] = FormatNumber(HeadingSpacing(style.Id).After),
            ["@@H1_SIZE@@"] = FormatNumber(HeadingSizes(style.Id)[0]),
            ["@@H2_SIZE@@"] = FormatNumber(HeadingSizes(style.Id)[1]),
            ["@@H3_SIZE@@"] = FormatNumber(HeadingSizes(style.Id)[2]),
            ["@@H4_SIZE@@"] = FormatNumber(HeadingSizes(style.Id)[3]),
            ["@@H5_SIZE@@"] = FormatNumber(HeadingSizes(style.Id)[4]),
            ["@@H6_SIZE@@"] = FormatNumber(HeadingSizes(style.Id)[5]),
            ["@@PAGE_DECORATIONS@@"] = BuildPageDecorations(style, scale),
        };

        var template = File.ReadAllText(templatePath);

        foreach (var (placeholder, value) in replacements)
        {
            template = template.Replace(placeholder, value, StringComparison.Ordinal);
        }

        if (template.Contains("@@", StringComparison.Ordinal))
        {
            throw new ConversionException($"Typst 模板存在未替换的占位符：{templatePath}");
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, template, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string BuildPageDecorations(DocumentStyle style, double scale)
    {
        var builder = new StringBuilder();
        var fontSize = FormatNumber(Math.Max(8, 9 * scale));

        if (style.HeaderEnabled)
        {
            builder.AppendLine("  header: context {");
            builder.AppendLine($"    set text(size: {fontSize}pt, fill: rgb(\"#666666\"))");
            builder.AppendLine("    block(width: 100%, below: 4pt)[");
            builder.AppendLine("      #align(center)[");
            builder.AppendLine("        $if(title)$$title$$endif$");
            builder.AppendLine("        $if(title)$$if(author)$ · $endif$$endif$");
            builder.AppendLine("        $for(author)$$if(author.name)$$author.name$$else$$author$$endif$$sep$, $endfor$");
            builder.AppendLine("      ]");
            builder.AppendLine("      #line(length: 100%, stroke: 0.5pt + rgb(\"#BFBFBF\"))");
            builder.AppendLine("    ]");
            builder.AppendLine("  },");
        }

        if (style.PageNumberEnabled)
        {
            var pageNumber = style.Id == "minimal"
                ? "#counter(page).display()"
                : "第 #counter(page).display() 页";

            builder.AppendLine("  footer: context align(center)[");
            builder.AppendLine($"    #text(size: {fontSize}pt, fill: rgb(\"#666666\"))[{pageNumber}]");
            builder.AppendLine("  ],");
        }

        if (builder.Length == 0)
        {
            return "  header: none,\n  footer: none,\n";
        }

        return builder.ToString();
    }

    private static string BuildFontList(string fontFamily, string styleId)
    {
        var fonts = new List<string> { Quote(fontFamily) };

        if (styleId == "academic")
        {
            fonts.Add(Quote("Times New Roman"));
            fonts.Add(Quote("Microsoft YaHei"));
        }
        else
        {
            fonts.Add(Quote("Segoe UI"));
            fonts.Add(Quote("Arial"));
            fonts.Add(Quote("Microsoft YaHei"));
        }

        return string.Join(", ", fonts.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string DefaultFontFamily(string styleId)
    {
        return styleId == "academic" ? "SimSun" : "Microsoft YaHei";
    }

    private static string Quote(string value)
    {
        return "\"" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private static double TitleSize(string styleId)
    {
        return styleId switch
        {
            "academic" => 1.7,
            "minimal" => 1.5,
            _ => 1.8,
        };
    }

    private static (double Before, double After) HeadingSpacing(string styleId)
    {
        return styleId switch
        {
            "academic" => (16, 8),
            "minimal" => (10, 4),
            _ => (14, 6),
        };
    }

    private static double[] HeadingSizes(string styleId)
    {
        return styleId switch
        {
            "academic" => new[] { 1.6, 1.35, 1.2, 1.1, 1.0, 0.95 },
            "minimal" => new[] { 1.5, 1.3, 1.15, 1.05, 1.0, 0.95 },
            _ => new[] { 1.8, 1.45, 1.25, 1.1, 1.0, 0.95 },
        };
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
