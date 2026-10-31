using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Core.Services;
using MarkdownDocumentConverter.Tests.Infrastructure;

namespace MarkdownDocumentConverter.Tests.Unit;

public sealed class OutputPathServiceTests
{
    private readonly OutputPathService _service = new();

    [Fact]
    public void Rename_policy_adds_a_number_until_unique()
    {
        using var directory = new TestOutputDirectory();
        var first = _service.Resolve("input.md", directory.Path, ".docx", OverwritePolicy.Rename);
        File.WriteAllText(first, "first");

        var second = _service.Resolve("input.md", directory.Path, ".docx", OverwritePolicy.Rename);
        File.WriteAllText(second, "second");

        var third = _service.Resolve("input.md", directory.Path, ".docx", OverwritePolicy.Rename);

        Assert.Equal("input.docx", Path.GetFileName(first));
        Assert.Equal("input (1).docx", Path.GetFileName(second));
        Assert.Equal("input (2).docx", Path.GetFileName(third));
    }

    [Fact]
    public void Overwrite_policy_reuses_existing_path()
    {
        using var directory = new TestOutputDirectory();
        var first = _service.Resolve("input.md", directory.Path, ".pdf", OverwritePolicy.Overwrite);
        File.WriteAllText(first, "existing");

        var second = _service.Resolve("input.md", directory.Path, ".pdf", OverwritePolicy.Overwrite);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Skip_policy_returns_existing_path_for_caller_to_skip()
    {
        using var directory = new TestOutputDirectory();
        var first = _service.Resolve("input.md", directory.Path, ".pdf", OverwritePolicy.Skip);
        File.WriteAllText(first, "existing");

        var second = _service.Resolve("input.md", directory.Path, ".pdf", OverwritePolicy.Skip);

        Assert.Equal(first, second);
    }
}
