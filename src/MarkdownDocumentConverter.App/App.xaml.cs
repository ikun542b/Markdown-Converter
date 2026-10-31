using Microsoft.UI.Xaml;

namespace MarkdownDocumentConverter.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            var directory = Path.Combine(Path.GetTempPath(), "MDC");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "crash.log"), e.Exception.ToString());
        }
        catch (Exception)
        {
            // 记录失败时不再抛出二次异常。
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow(new AppServices());
        _window.Activate();
    }
}
