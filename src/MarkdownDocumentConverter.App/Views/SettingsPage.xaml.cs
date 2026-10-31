using MarkdownDocumentConverter.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MarkdownDocumentConverter.App.Views;

public sealed partial class SettingsPage : Page
{
    private readonly Window _owner;

    public SettingsPage(AppServices services, Window owner)
    {
        _owner = owner;
        ViewModel = new SettingsViewModel(services);
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; }

    private async void ChooseOutputDirectory_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };

        picker.FileTypeFilter.Add("*");

        var windowHandle = WindowNative.GetWindowHandle(_owner);
        InitializeWithWindow.Initialize(picker, windowHandle);

        var folder = await picker.PickSingleFolderAsync();

        if (folder is not null)
        {
            ViewModel.OutputDirectory = folder.Path;
            ViewModel.UseSourceDirectory = false;
        }
    }
}
