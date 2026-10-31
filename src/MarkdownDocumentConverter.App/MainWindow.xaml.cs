using MarkdownDocumentConverter.App.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace MarkdownDocumentConverter.App;

public sealed partial class MainWindow : Window
{
    private readonly AppServices _services;
    private readonly ConvertPage _convertPage;
    private readonly SettingsPage _settingsPage;

    public MainWindow(AppServices services)
    {
        _services = services;

        InitializeComponent();

        Title = "Markdown Document Converter";
        AppWindow.MoveAndResize(new RectInt32(50, 50, 1400, 900));

        _convertPage = new ConvertPage(services, this);
        _settingsPage = new SettingsPage(services, this);
        ContentHost.Content = _convertPage;

        var startupFiles = Environment.GetCommandLineArgs()
            .Skip(1)
            .Where(File.Exists)
            .ToArray();

        if (startupFiles.Length > 0)
        {
            _convertPage.ViewModel.AddFiles(startupFiles);
        }
    }

    private async void Navigation_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _services.LoadSettingsAsync();
        }
        catch (Exception ex)
        {
            _convertPage.ViewModel.ReportError($"加载设置失败：{ex.Message}");
        }
    }

    private void Navigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem { Tag: string tag })
        {
            return;
        }

        if (ContentHost is null || _settingsPage is null || _convertPage is null)
        {
            return;
        }

        ContentHost.Content = tag == "settings" ? _settingsPage : _convertPage;
    }
}
