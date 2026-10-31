using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.App.ViewModels;

public sealed class FileItemViewModel : ObservableObject
{
    private ConversionStatus _status = ConversionStatus.Waiting;
    private string? _errorMessage;

    public FileItemViewModel(string inputPath)
    {
        InputPath = Path.GetFullPath(inputPath);
        FileName = Path.GetFileName(inputPath);
        SizeText = FormatSize(new FileInfo(inputPath).Length);
    }

    public string InputPath { get; }

    public string FileName { get; }

    public string Directory => Path.GetDirectoryName(InputPath) ?? string.Empty;

    public string SizeText { get; }

    public ConversionStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsFinished));
            }
        }
    }

    public string StatusText => Status switch
    {
        ConversionStatus.Waiting => "等待转换",
        ConversionStatus.Running => "转换中",
        ConversionStatus.Success => "成功",
        ConversionStatus.Failed => "失败",
        ConversionStatus.Cancelled => "已取消",
        ConversionStatus.Skipped => "已跳过",
        _ => Status.ToString(),
    };

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string? DocxPath { get; private set; }

    public string? PdfPath { get; private set; }

    public bool IsFinished => Status is
        ConversionStatus.Success or
        ConversionStatus.Failed or
        ConversionStatus.Cancelled or
        ConversionStatus.Skipped;

    public string? LastOutputPath => PdfPath ?? DocxPath;

    public void MarkRunning()
    {
        ErrorMessage = null;
        Status = ConversionStatus.Running;
    }

    public void MarkCancelled(string? message = null)
    {
        ErrorMessage = message;
        Status = ConversionStatus.Cancelled;
    }

    public void MarkFailed(string message)
    {
        ErrorMessage = message;
        Status = ConversionStatus.Failed;
    }

    public void ApplyResult(ConversionResult result)
    {
        DocxPath = result.DocxPath;
        PdfPath = result.PdfPath;
        ErrorMessage = result.ErrorMessage;
        Status = result.Status;
    }

    public void ResetForRetry()
    {
        if (Status is ConversionStatus.Failed or ConversionStatus.Cancelled)
        {
            DocxPath = null;
            PdfPath = null;
            ErrorMessage = null;
            Status = ConversionStatus.Waiting;
        }
    }

    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
            _ => $"{bytes / (1024.0 * 1024.0):0.#} MB",
        };
    }
}
