using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MarkdownDocumentConverter.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private const string SampleMarkdown = """
        ---
        title: Phase 0 测试文档
        author: Markdown Document Converter
        lang: zh-CN
        ---

        # 一级标题

        这是 **Markdown** 转换测试，包含中文与 English。

        | 项目 | 结果 |
        |---|---|
        | DOCX | 待验证 |
        | PDF | 待验证 |

        行内公式：$E=mc^2$

        $$
        E = mc^2
        $$
        """;

    private readonly AppServices _services;
    private string _statusText = "点击“检测引擎”开始环境验证。";
    private bool _isBusy;

    public MainWindowViewModel(AppServices services)
    {
        _services = services;
        DetectEnginesCommand = new AsyncRelayCommand(DetectEnginesAsync, () => !IsBusy);
        RunSmokeTestCommand = new AsyncRelayCommand(RunSmokeTestAsync, () => !IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                DetectEnginesCommand.RaiseCanExecuteChanged();
                RunSmokeTestCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand DetectEnginesCommand { get; }

    public AsyncRelayCommand RunSmokeTestCommand { get; }

    private async Task DetectEnginesAsync()
    {
        IsBusy = true;
        var report = new StringBuilder();

        try
        {
            report.AppendLine("运行环境");
            report.AppendLine($"  .NET          : {Environment.Version}");
            report.AppendLine($"  OS            : {RuntimeInformation.OSDescription}");
            report.AppendLine($"  架构          : {RuntimeInformation.ProcessArchitecture}");
            report.AppendLine($"  基目录        : {AppContext.BaseDirectory}");
            report.AppendLine();

            report.AppendLine("转换引擎");
            report.AppendLine($"  资源根目录    : {_services.Locator.ResourceRoot ?? "未找到"}");
            await AppendEngineStatusAsync(report, "Pandoc", "pandoc.exe", _services.Pandoc.GetVersionAsync);
            await AppendEngineStatusAsync(report, "Typst", "typst.exe", _services.Typst.GetVersionAsync);
            report.AppendLine();
            report.AppendLine("检测完成。");
        }
        catch (Exception ex)
        {
            report.AppendLine();
            report.AppendLine($"检测失败：{ex.Message}");
        }
        finally
        {
            StatusText = report.ToString().TrimEnd();
            IsBusy = false;
        }
    }

    private async Task AppendEngineStatusAsync(
        StringBuilder report,
        string displayName,
        string fileName,
        Func<CancellationToken, Task<string>> getVersion)
    {
        if (!_services.Locator.TryGetEnginePath(fileName, out var path))
        {
            report.AppendLine($"  {displayName,-6}      : 未找到 {fileName}");
            return;
        }

        try
        {
            var version = await getVersion(CancellationToken.None);
            report.AppendLine($"  {displayName,-6}      : {version}");
            report.AppendLine($"    路径        : {path}");
        }
        catch (Exception ex)
        {
            report.AppendLine($"  {displayName,-6}      : 检测失败 - {ex.Message}");
        }
    }

    private async Task RunSmokeTestAsync()
    {
        IsBusy = true;
        StatusText = "正在运行最小转换，请稍候...";

        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            "MDC",
            "phase0-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);

        var inputPath = Path.Combine(outputDirectory, "sample.md");
        await File.WriteAllTextAsync(inputPath, SampleMarkdown);

        var report = new StringBuilder();
        report.AppendLine("最小转换验证");

        try
        {
            var docxPath = Path.Combine(outputDirectory, "sample.docx");
            var word = await _services.WordPipeline.ConvertAsync(inputPath, docxPath);
            report.AppendLine($"  DOCX          : 成功（{word.SizeBytes:N0} 字节）");
            report.AppendLine($"    输出        : {docxPath}");

            var pdfPath = Path.Combine(outputDirectory, "sample.pdf");
            var pdf = await _services.PdfPipeline.ConvertAsync(inputPath, pdfPath);
            report.AppendLine($"  PDF           : 成功（{pdf.SizeBytes:N0} 字节）");
            report.AppendLine($"    输出        : {pdfPath}");
            report.AppendLine();
            report.AppendLine($"输出目录：{outputDirectory}");
        }
        catch (Exception ex)
        {
            report.AppendLine();
            report.AppendLine($"最小转换失败：{ex.Message}");
            report.AppendLine($"工作目录：{outputDirectory}");
        }
        finally
        {
            StatusText = report.ToString().TrimEnd();
            IsBusy = false;
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
