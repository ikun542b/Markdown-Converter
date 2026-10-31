using System.IO.Compression;
using MarkdownDocumentConverter.Core.Exceptions;

namespace MarkdownDocumentConverter.Core.Pipelines;

public static class OutputValidator
{
    public static void EnsureNonEmpty(string path, string kind)
    {
        var file = new FileInfo(path);

        if (!file.Exists || file.Length == 0)
        {
            throw new ConversionException($"{kind} 输出文件未生成或为空：{path}");
        }
    }

    public static void EnsureDocx(string path)
    {
        try
        {
            using var archive = ZipFile.OpenRead(path);
            _ = archive.GetEntry("word/document.xml")
                ?? throw new ConversionException($"DOCX 结构无效，缺少 word/document.xml：{path}");
        }
        catch (InvalidDataException ex)
        {
            throw new ConversionException($"DOCX 文件无法作为 ZIP/OOXML 读取：{path}", ex);
        }
    }

    public static void EnsurePdf(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[5];

        if (stream.Read(header) != header.Length || !header.SequenceEqual("%PDF-"u8))
        {
            throw new ConversionException($"PDF 文件头无效：{path}");
        }
    }
}
