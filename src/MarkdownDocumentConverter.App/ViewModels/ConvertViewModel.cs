using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Core.Services;
using Windows.System;

namespace MarkdownDocumentConverter.App.ViewModels;

public sealed class ConvertViewModel : ObservableObject
{
    private readonly AppServices _services;
    private readonly ObservableCollection<FileItemViewModel> _files = new();
    private CancellationTokenSource? _cancellation;
    private OptionItem<PageSize> _selectedPageSize;
    private OptionItem<string> _selectedStyle;
    private OptionItem<string?> _selectedFont;
    private OptionItem<double> _selectedFontScale;
    private bool _convertWord = true;
    private bool _convertPdf = true;
    private bool _useSourceDirectory = true;
    private string _outputDirectory = string.Empty;
    private bool _recursive = true;
    private bool _isConverting;
    private string _statusMessage = "添加 Markdown 文件后开始转换。";
    private string _progressText = string.Empty;
    private double _progressValue;

    public ConvertViewModel(AppServices services)
    {
        _services = services;

        PageSizeOptions = OptionCatalog.PageSizes;
        StyleOptions = OptionCatalog.Styles;
        FontScaleOptions = OptionCatalog.FontScales;
        FontOptions = OptionCatalog.BuildFontOptions(services.FontService.GetInstalledFonts());

        _selectedPageSize = PageSizeOptions[0];
        _selectedStyle = StyleOptions[0];
        _selectedFont = FontOptions[0];
        _selectedFontScale = FontScaleOptions.First(option => option.Value == 1.0);

        StartCommand = new AsyncRelayCommand(() => StartAsync(), () => !IsConverting && Files.Count > 0);
        CancelCommand = new RelayCommand(Cancel, () => IsConverting);
        ClearCommand = new RelayCommand(Clear, () => !IsConverting && Files.Count > 0);
        RetryFailedCommand = new AsyncRelayCommand(RetryFailedAsync, () => !IsConverting && HasRetryableFiles());

        Files.CollectionChanged += OnFilesChanged;
        _services.SettingsChanged += OnSettingsChanged;

        ApplySettings(_services.Settings);
    }

    public ObservableCollection<FileItemViewModel> Files => _files;

    public IReadOnlyList<OptionItem<PageSize>> PageSizeOptions { get; }

    public IReadOnlyList<OptionItem<string>> StyleOptions { get; }

    public IReadOnlyList<OptionItem<string?>> FontOptions { get; }

    public IReadOnlyList<OptionItem<double>> FontScaleOptions { get; }

    public OptionItem<PageSize> SelectedPageSize
    {
        get => _selectedPageSize;
        set => SetProperty(ref _selectedPageSize, value);
    }

    public OptionItem<string> SelectedStyle
    {
        get => _selectedStyle;
        set => SetProperty(ref _selectedStyle, value);
    }

    public OptionItem<string?> SelectedFont
    {
        get => _selectedFont;
        set => SetProperty(ref _selectedFont, value);
    }

    public OptionItem<double> SelectedFontScale
    {
        get => _selectedFontScale;
        set => SetProperty(ref _selectedFontScale, value);
    }

    public bool ConvertWord
    {
        get => _convertWord;
        set => SetProperty(ref _convertWord, value);
    }

    public bool ConvertPdf
    {
        get => _convertPdf;
        set => SetProperty(ref _convertPdf, value);
    }

    public bool UseSourceDirectory
    {
        get => _useSourceDirectory;
        set
        {
            if (SetProperty(ref _useSourceDirectory, value))
            {
                OnPropertyChanged(nameof(OutputDirectorySummary));
            }
        }
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set
        {
            if (SetProperty(ref _outputDirectory, value))
            {
                OnPropertyChanged(nameof(OutputDirectorySummary));
            }
        }
    }

    public string OutputDirectorySummary => UseSourceDirectory
        ? "与源文件相同"
        : string.IsNullOrWhiteSpace(OutputDirectory)
            ? "请选择输出目录"
            : OutputDirectory;

    public bool Recursive
    {
        get => _recursive;
        set => SetProperty(ref _recursive, value);
    }

    public bool IsConverting
    {
        get => _isConverting;
        private set
        {
            if (SetProperty(ref _isConverting, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ProgressText
    {
        get => _progressText;
        private set => SetProperty(ref _progressText, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public AsyncRelayCommand StartCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand ClearCommand { get; }

    public AsyncRelayCommand RetryFailedCommand { get; }

    public void ReportError(string message)
    {
        StatusMessage = message;
    }

    public int AddFiles(IEnumerable<string> paths)
    {
        var added = 0;

        foreach (var path in paths)
        {
            if (!IsMarkdownFile(path) || !File.Exists(path))
            {
                continue;
            }

            if (AddFile(new FileItemViewModel(Path.GetFullPath(path))))
            {
                added++;
            }
        }

        if (added > 0)
        {
            StatusMessage = $"已添加 {added} 个文件，共 {Files.Count} 个。";
        }

        return added;
    }

    public async Task<int> AddFolderAsync(string folderPath, bool recursive)
    {
        if (!Directory.Exists(folderPath))
        {
            StatusMessage = "文件夹不存在。";
            return 0;
        }

        try
        {
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var root = Path.GetFullPath(folderPath);

            var paths = await Task.Run(() =>
                Directory.EnumerateFiles(root, "*", option)
                    .Where(IsMarkdownFile)
                    .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase)
                    .ToArray());

            var added = 0;

            foreach (var path in paths)
            {
                var fullPath = Path.GetFullPath(path);
                var parent = Path.GetDirectoryName(fullPath) ?? root;
                var relativeDirectory = Path.GetRelativePath(root, parent);

                if (relativeDirectory == ".")
                {
                    relativeDirectory = string.Empty;
                }

                if (AddFile(new FileItemViewModel(fullPath, relativeDirectory)))
                {
                    added++;
                }
            }

            if (added == 0)
            {
                StatusMessage = "文件夹中没有找到新的 Markdown 文件。";
            }

            return added;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"无法扫描文件夹：{ex.Message}";
            return 0;
        }
    }

    public async Task StartAsync(IReadOnlyList<FileItemViewModel>? requestedTargets = null)
    {
        if (IsConverting)
        {
            return;
        }

        var formats = OutputFormat.None;

        if (ConvertWord)
        {
            formats |= OutputFormat.Docx;
        }

        if (ConvertPdf)
        {
            formats |= OutputFormat.Pdf;
        }

        if (formats == OutputFormat.None)
        {
            StatusMessage = "请至少选择一种输出格式。";
            return;
        }

        var targets = (requestedTargets ?? Files)
            .Where(file => requestedTargets is not null
                || file.Status is ConversionStatus.Waiting or ConversionStatus.Failed or ConversionStatus.Cancelled)
            .ToList();

        if (targets.Count == 0)
        {
            StatusMessage = "没有待转换的文件。";
            return;
        }

        if (!UseSourceDirectory && string.IsNullOrWhiteSpace(OutputDirectory))
        {
            StatusMessage = "请选择输出目录。";
            return;
        }

        IsConverting = true;
        ProgressValue = 0;
        ProgressText = $"0 / {targets.Count}";
        var settings = _services.Settings;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;

        var succeeded = 0;
        var failed = 0;
        var fileByPath = new Dictionary<string, FileItemViewModel>(StringComparer.OrdinalIgnoreCase);
        var requests = new List<ConversionRequest>();

        foreach (var file in targets)
        {
            var outputDirectory = UseSourceDirectory
                ? file.Directory
                : Path.Combine(OutputDirectory, file.RelativeDirectory);

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                file.MarkFailed("无法确定输出目录。");
                failed++;
                continue;
            }

            fileByPath[file.InputPath] = file;
            requests.Add(new ConversionRequest
            {
                InputPath = file.InputPath,
                OutputDirectory = outputDirectory,
                Formats = formats,
                PageSize = SelectedPageSize.Value,
                StyleId = SelectedStyle.Value,
                FontFamily = SelectedFont.Value,
                FontScale = SelectedFontScale.Value,
                OverwritePolicy = settings.OverwritePolicy,
                Timeout = TimeSpan.FromSeconds(settings.ConversionTimeoutSeconds),
            });
        }

        var progress = new Progress<ConversionProgress>(item =>
        {
            if (item.Stage is ConversionStage.Word or ConversionStage.Pdf
                && fileByPath.TryGetValue(item.InputPath, out var current)
                && current.Status == ConversionStatus.Waiting)
            {
                current.MarkRunning();
            }

            ProgressText = $"{item.Message}（{item.CompletedCount}/{item.TotalCount}）";
        });

        try
        {
            if (requests.Count > 0)
            {
                StatusMessage = $"正在转换 {requests.Count} 个文件（并发 {settings.ParallelJobs}）。";

                var results = await _services.Batch.ConvertAsync(
                    requests,
                    settings.ParallelJobs,
                    progress,
                    cancellation.Token);

                foreach (var result in results)
                {
                    if (!fileByPath.TryGetValue(result.InputPath, out var file))
                    {
                        continue;
                    }

                    file.ApplyResult(result);

                    if (result.Status == ConversionStatus.Success)
                    {
                        succeeded++;
                    }
                    else
                    {
                        failed++;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            foreach (var file in fileByPath.Values.Where(file => !file.IsFinished))
            {
                file.MarkCancelled("转换已取消。");
            }

            StatusMessage = "转换已取消。";
        }
        finally
        {
            IsConverting = false;
            _cancellation = null;

            var completed = succeeded + failed;
            ProgressValue = completed * 100.0 / targets.Count;
            ProgressText = $"{completed} / {targets.Count}";

            var summary = $"完成 {completed} 个文件：成功 {succeeded}，失败 {failed}。";
            StatusMessage = cancellation.IsCancellationRequested
                ? $"已取消。{summary}"
                : summary;

            if (settings.OpenOutputFolderAfterComplete && succeeded > 0)
            {
                var folder = UseSourceDirectory
                    ? targets.FirstOrDefault(file => file.Status == ConversionStatus.Success)?.Directory
                    : OutputDirectory;

                if (!string.IsNullOrWhiteSpace(folder))
                {
                    await TryOpenFolderAsync(folder);
                }
            }
        }
    }

    private bool AddFile(FileItemViewModel file)
    {
        if (Files.Any(existing =>
                string.Equals(existing.InputPath, file.InputPath, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        file.RetryCommand = new AsyncRelayCommand(() => RetrySingleAsync(file));
        Files.Add(file);
        return true;
    }

    private async Task RetrySingleAsync(FileItemViewModel file)
    {
        if (IsConverting)
        {
            return;
        }

        file.ResetForRetry();
        await StartAsync(new[] { file });
    }

    public void Clear()
    {
        if (IsConverting)
        {
            return;
        }

        Files.Clear();
        StatusMessage = "列表已清空。";
        ProgressValue = 0;
        ProgressText = string.Empty;
    }

    private void Cancel()
    {
        _cancellation?.Cancel();
        StatusMessage = "正在取消转换...";
    }

    private async Task RetryFailedAsync()
    {
        foreach (var file in Files)
        {
            file.ResetForRetry();
        }

        await StartAsync();
    }

    private bool HasRetryableFiles()
    {
        return Files.Any(file => file.Status is ConversionStatus.Failed or ConversionStatus.Cancelled);
    }

    private void ApplySettings(AppSettings settings)
    {
        SelectedPageSize = PageSizeOptions.FirstOrDefault(option => option.Value == settings.PageSize)
            ?? PageSizeOptions[0];
        SelectedStyle = StyleOptions.FirstOrDefault(option =>
            string.Equals(option.Value, settings.Style, StringComparison.OrdinalIgnoreCase))
            ?? StyleOptions[0];
        SelectedFont = FontOptions.FirstOrDefault(option =>
            string.Equals(option.Value, settings.FontFamily, StringComparison.OrdinalIgnoreCase))
            ?? FontOptions[0];
        SelectedFontScale = FontScaleOptions.FirstOrDefault(option =>
            Math.Abs(option.Value - settings.FontScale) < 0.001)
            ?? FontScaleOptions.First(option => option.Value == 1.0);

        ConvertWord = settings.DefaultFormats.Contains(OutputFormat.Docx);
        ConvertPdf = settings.DefaultFormats.Contains(OutputFormat.Pdf);

        if (!ConvertWord && !ConvertPdf)
        {
            ConvertWord = true;
            ConvertPdf = true;
        }

        UseSourceDirectory = !string.Equals(
            settings.DefaultOutputMode,
            "fixed",
            StringComparison.OrdinalIgnoreCase);
        OutputDirectory = settings.DefaultOutputDirectory ?? string.Empty;
        Recursive = settings.Recursive;
        RaiseCommandStates();
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        ApplySettings(_services.Settings);
    }

    private void OnFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        StartCommand.RaiseCanExecuteChanged();
        ClearCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
        RetryFailedCommand.RaiseCanExecuteChanged();
    }

    private static bool IsMarkdownFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task TryOpenFolderAsync(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                await Launcher.LaunchFolderPathAsync(folder);
            }
        }
        catch (Exception)
        {
            // 打开资源管理器失败不影响转换结果。
        }
    }
}
