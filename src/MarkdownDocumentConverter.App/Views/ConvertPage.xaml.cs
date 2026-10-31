using MarkdownDocumentConverter.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace MarkdownDocumentConverter.App.Views;

public sealed partial class ConvertPage : Page
{
    private readonly Window _owner;

    public ConvertPage(AppServices services, Window owner)
    {
        _owner = owner;
        ViewModel = new ConvertViewModel(services);
        InitializeComponent();
    }

    public ConvertViewModel ViewModel { get; }

    private async void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List,
        };

        picker.FileTypeFilter.Add(".md");
        picker.FileTypeFilter.Add(".markdown");
        InitializePicker(picker);

        var files = await picker.PickMultipleFilesAsync();

        if (files.Count > 0)
        {
            ViewModel.AddFiles(files.Select(file => file.Path));
        }
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };

        picker.FileTypeFilter.Add("*");
        InitializePicker(picker);

        var folder = await picker.PickSingleFolderAsync();

        if (folder is not null)
        {
            await ViewModel.AddFolderAsync(folder.Path, ViewModel.Recursive);
        }
    }

    private async void ChooseOutputDirectory_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };

        picker.FileTypeFilter.Add("*");
        InitializePicker(picker);

        var folder = await picker.PickSingleFolderAsync();

        if (folder is not null)
        {
            ViewModel.OutputDirectory = folder.Path;
            ViewModel.UseSourceDirectory = false;
        }
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "添加 Markdown 文件";
        e.DragUIOverride.IsCaptionVisible = true;
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();

        foreach (var item in items)
        {
            switch (item)
            {
                case StorageFile file when !string.IsNullOrWhiteSpace(file.Path):
                    ViewModel.AddFiles(new[] { file.Path });
                    break;

                case StorageFolder folder when !string.IsNullOrWhiteSpace(folder.Path):
                    await ViewModel.AddFolderAsync(folder.Path, ViewModel.Recursive);
                    break;
            }
        }
    }

    private async void Queue_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (Queue.SelectedItem is not FileItemViewModel item
            || item.LastOutputPath is not string path
            || !File.Exists(path))
        {
            return;
        }

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            await Launcher.LaunchFileAsync(file);
        }
        catch (Exception)
        {
            // 打开失败时保留当前界面状态。
        }
    }

    private void InitializePicker(object picker)
    {
        var windowHandle = WindowNative.GetWindowHandle(_owner);
        InitializeWithWindow.Initialize(picker, windowHandle);
    }
}
