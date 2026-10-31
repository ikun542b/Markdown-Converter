using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppServices _services;
    private OptionItem<PageSize> _selectedPageSize;
    private OptionItem<string> _selectedStyle;
    private OptionItem<string?> _selectedFont;
    private OptionItem<double> _selectedFontScale;
    private OptionItem<OverwritePolicy> _selectedOverwritePolicy;
    private OptionItem<int> _selectedParallelJobs;
    private bool _defaultWord = true;
    private bool _defaultPdf = true;
    private bool _useSourceDirectory = true;
    private string _outputDirectory = string.Empty;
    private bool _recursive = true;
    private bool _openOutputFolderAfterComplete = true;
    private string _statusMessage = "设置会自动保存到用户配置目录。";

    public SettingsViewModel(AppServices services)
    {
        _services = services;

        PageSizeOptions = OptionCatalog.PageSizes;
        StyleOptions = OptionCatalog.Styles;
        FontScaleOptions = OptionCatalog.FontScales;
        OverwritePolicyOptions = OptionCatalog.OverwritePolicies;
        ParallelJobOptions = new[]
        {
            new OptionItem<int>("1（默认）", 1),
            new OptionItem<int>("2", 2),
            new OptionItem<int>("3", 3),
            new OptionItem<int>("4", 4),
        };

        FontOptions = OptionCatalog.BuildFontOptions(services.FontService.GetInstalledFonts());

        _selectedPageSize = PageSizeOptions[0];
        _selectedStyle = StyleOptions[0];
        _selectedFont = FontOptions[0];
        _selectedFontScale = FontScaleOptions.First(option => option.Value == 1.0);
        _selectedOverwritePolicy = OverwritePolicyOptions[0];
        _selectedParallelJobs = ParallelJobOptions[0];

        ApplySettings(_services.Settings);
        _services.SettingsChanged += OnSettingsChanged;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public IReadOnlyList<OptionItem<PageSize>> PageSizeOptions { get; }

    public IReadOnlyList<OptionItem<string>> StyleOptions { get; }

    public IReadOnlyList<OptionItem<string?>> FontOptions { get; }

    public IReadOnlyList<OptionItem<double>> FontScaleOptions { get; }

    public IReadOnlyList<OptionItem<OverwritePolicy>> OverwritePolicyOptions { get; }

    public IReadOnlyList<OptionItem<int>> ParallelJobOptions { get; }

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

    public OptionItem<OverwritePolicy> SelectedOverwritePolicy
    {
        get => _selectedOverwritePolicy;
        set => SetProperty(ref _selectedOverwritePolicy, value);
    }

    public OptionItem<int> SelectedParallelJobs
    {
        get => _selectedParallelJobs;
        set => SetProperty(ref _selectedParallelJobs, value);
    }

    public bool DefaultWord
    {
        get => _defaultWord;
        set => SetProperty(ref _defaultWord, value);
    }

    public bool DefaultPdf
    {
        get => _defaultPdf;
        set => SetProperty(ref _defaultPdf, value);
    }

    public bool UseSourceDirectory
    {
        get => _useSourceDirectory;
        set => SetProperty(ref _useSourceDirectory, value);
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set => SetProperty(ref _outputDirectory, value);
    }

    public bool Recursive
    {
        get => _recursive;
        set => SetProperty(ref _recursive, value);
    }

    public bool OpenOutputFolderAfterComplete
    {
        get => _openOutputFolderAfterComplete;
        set => SetProperty(ref _openOutputFolderAfterComplete, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public AsyncRelayCommand SaveCommand { get; }

    private async Task SaveAsync()
    {
        if (!DefaultWord && !DefaultPdf)
        {
            StatusMessage = "请至少选择一种默认输出格式。";
            return;
        }

        if (!UseSourceDirectory && string.IsNullOrWhiteSpace(OutputDirectory))
        {
            StatusMessage = "请选择默认输出目录。";
            return;
        }

        try
        {
            var settings = BuildSettings();
            await _services.SaveSettingsAsync(settings);
            StatusMessage = "设置已保存。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存设置失败：{ex.Message}";
        }
    }

    private AppSettings BuildSettings()
    {
        var formats = new List<OutputFormat>();

        if (DefaultWord)
        {
            formats.Add(OutputFormat.Docx);
        }

        if (DefaultPdf)
        {
            formats.Add(OutputFormat.Pdf);
        }

        return new AppSettings
        {
            ConfigVersion = AppSettings.CurrentConfigVersion,
            Theme = "system",
            DefaultFormats = formats,
            DefaultOutputMode = UseSourceDirectory ? "same_directory" : "fixed",
            DefaultOutputDirectory = UseSourceDirectory ? null : OutputDirectory,
            PageSize = SelectedPageSize.Value,
            Style = SelectedStyle.Value,
            FontFamily = SelectedFont.Value,
            FontScale = SelectedFontScale.Value,
            Recursive = Recursive,
            OverwritePolicy = SelectedOverwritePolicy.Value,
            OpenOutputFolderAfterComplete = OpenOutputFolderAfterComplete,
            ParallelJobs = SelectedParallelJobs.Value,
            RetainIntermediateFiles = _services.Settings.RetainIntermediateFiles,
            ConversionTimeoutSeconds = _services.Settings.ConversionTimeoutSeconds,
            LogRetentionCount = _services.Settings.LogRetentionCount,
            PandocPath = _services.Settings.PandocPath,
            TypstPath = _services.Settings.TypstPath,
        };
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
        SelectedOverwritePolicy = OverwritePolicyOptions.FirstOrDefault(option =>
            option.Value == settings.OverwritePolicy)
            ?? OverwritePolicyOptions[0];
        SelectedParallelJobs = ParallelJobOptions.FirstOrDefault(option =>
            option.Value == settings.ParallelJobs)
            ?? ParallelJobOptions[0];

        DefaultWord = settings.DefaultFormats.Contains(OutputFormat.Docx);
        DefaultPdf = settings.DefaultFormats.Contains(OutputFormat.Pdf);

        if (!DefaultWord && !DefaultPdf)
        {
            DefaultWord = true;
            DefaultPdf = true;
        }

        UseSourceDirectory = !string.Equals(
            settings.DefaultOutputMode,
            "fixed",
            StringComparison.OrdinalIgnoreCase);
        OutputDirectory = settings.DefaultOutputDirectory ?? string.Empty;
        Recursive = settings.Recursive;
        OpenOutputFolderAfterComplete = settings.OpenOutputFolderAfterComplete;
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        ApplySettings(_services.Settings);
    }
}
