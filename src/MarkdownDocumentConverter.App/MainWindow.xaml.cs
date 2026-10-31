using MarkdownDocumentConverter.App.ViewModels;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace MarkdownDocumentConverter.App;

public sealed partial class MainWindow : Window
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(AppServices services)
    {
        ViewModel = new MainWindowViewModel(services);
        InitializeComponent();

        Title = "Markdown Document Converter";
        AppWindow.Resize(new SizeInt32(1000, 760));
    }
}
