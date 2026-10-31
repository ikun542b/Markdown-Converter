namespace MarkdownDocumentConverter.Tests.Infrastructure;

internal static class TestPaths
{
    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string Fixture(string fileName)
    {
        return Path.Combine(RepositoryRoot, "tests", "fixtures", fileName);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "MarkdownDocumentConverter.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("找不到仓库根目录（MarkdownDocumentConverter.sln）。");
    }
}
