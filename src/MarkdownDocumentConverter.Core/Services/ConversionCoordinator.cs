using MarkdownDocumentConverter.Core.Abstractions;
using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Core.Pipelines;

namespace MarkdownDocumentConverter.Core.Services;

public sealed class ConversionCoordinator
{
    private readonly WordPipeline _wordPipeline;
    private readonly PdfPipeline _pdfPipeline;
    private readonly OutputPathService _outputPathService;
    private readonly IAppLogger? _logger;

    public ConversionCoordinator(
        WordPipeline wordPipeline,
        PdfPipeline pdfPipeline,
        OutputPathService? outputPathService = null,
        IAppLogger? logger = null)
    {
        _wordPipeline = wordPipeline;
        _pdfPipeline = pdfPipeline;
        _outputPathService = outputPathService ?? new OutputPathService();
        _logger = logger;
    }

    public async Task<ConversionResult> ConvertAsync(
        ConversionRequest request,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var timeout = request.Timeout > TimeSpan.Zero
            ? request.Timeout
            : TimeSpan.FromMinutes(2);

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        var token = linkedSource.Token;

        var job = new ConversionJob(request.InputPath, request.OutputDirectory, request.Formats);
        var total = CountFormats(request.Formats);
        var completed = 0;

        _logger?.Info(
            $"开始转换：{request.InputPath} -> {request.OutputDirectory} [{request.Formats}]，超时 {timeout.TotalSeconds:0} 秒。");

        if (request.Formats.HasFlag(OutputFormat.Docx))
        {
            job.MarkRunning(OutputFormat.Docx);
            Report(progress, job, ConversionStage.Word, OutputFormat.Docx, completed, total, "正在生成 Word。");

            var outputPath = _outputPathService.Resolve(
                request.InputPath,
                request.OutputDirectory,
                ".docx",
                request.OverwritePolicy);

            if (request.OverwritePolicy == OverwritePolicy.Skip && File.Exists(outputPath))
            {
                job.MarkSkipped(OutputFormat.Docx, "同名 DOCX 已存在，已跳过。");
                _logger?.Info($"跳过已有 DOCX：{outputPath}");
            }
            else
            {
                try
                {
                    var artifact = await _wordPipeline.ConvertAsync(request, outputPath, token)
                        .ConfigureAwait(false);

                    job.MarkSucceeded(OutputFormat.Docx, artifact.OutputPath, artifact.Duration);
                    _logger?.Info($"DOCX 完成：{artifact.OutputPath}（{artifact.SizeBytes} 字节）");
                }
                catch (OperationCanceledException) when (
                    timeoutSource.IsCancellationRequested
                    && !cancellationToken.IsCancellationRequested)
                {
                    job.MarkFailed(OutputFormat.Docx, $"Word 转换超时（{timeout.TotalSeconds:0} 秒）。");
                    _logger?.Error($"DOCX 转换超时：{request.InputPath}");
                }
                catch (OperationCanceledException)
                {
                    job.MarkCancelled(OutputFormat.Docx);
                    _logger?.Info($"DOCX 转换已取消：{request.InputPath}");
                    Report(progress, job, ConversionStage.Cancelled, OutputFormat.Docx, completed, total, "已取消。");
                    return job.ToResult();
                }
                catch (Exception ex)
                {
                    job.MarkFailed(OutputFormat.Docx, ErrorMapper.ToUserError(ex).UserMessage);
                    _logger?.Error($"DOCX 转换失败：{request.InputPath}", ex);
                }
            }

            completed++;
            Report(progress, job, ConversionStage.Word, OutputFormat.Docx, completed, total, job.WordStatus.ToString());
        }

        if (request.Formats.HasFlag(OutputFormat.Pdf))
        {
            job.MarkRunning(OutputFormat.Pdf);
            Report(progress, job, ConversionStage.Pdf, OutputFormat.Pdf, completed, total, "正在生成 PDF。");

            var outputPath = _outputPathService.Resolve(
                request.InputPath,
                request.OutputDirectory,
                ".pdf",
                request.OverwritePolicy);

            if (request.OverwritePolicy == OverwritePolicy.Skip && File.Exists(outputPath))
            {
                job.MarkSkipped(OutputFormat.Pdf, "同名 PDF 已存在，已跳过。");
                _logger?.Info($"跳过已有 PDF：{outputPath}");
            }
            else
            {
                try
                {
                    var artifact = await _pdfPipeline.ConvertAsync(request, outputPath, token)
                        .ConfigureAwait(false);

                    job.MarkSucceeded(OutputFormat.Pdf, artifact.OutputPath, artifact.Duration);
                    _logger?.Info($"PDF 完成：{artifact.OutputPath}（{artifact.SizeBytes} 字节）");
                }
                catch (OperationCanceledException) when (
                    timeoutSource.IsCancellationRequested
                    && !cancellationToken.IsCancellationRequested)
                {
                    job.MarkFailed(OutputFormat.Pdf, $"PDF 转换超时（{timeout.TotalSeconds:0} 秒）。");
                    _logger?.Error($"PDF 转换超时：{request.InputPath}");
                }
                catch (OperationCanceledException)
                {
                    job.MarkCancelled(OutputFormat.Pdf);
                    _logger?.Info($"PDF 转换已取消：{request.InputPath}");
                    Report(progress, job, ConversionStage.Cancelled, OutputFormat.Pdf, completed, total, "已取消。");
                    return job.ToResult();
                }
                catch (Exception ex)
                {
                    job.MarkFailed(OutputFormat.Pdf, ErrorMapper.ToUserError(ex).UserMessage);
                    _logger?.Error($"PDF 转换失败：{request.InputPath}", ex);
                }
            }

            completed++;
            Report(progress, job, ConversionStage.Pdf, OutputFormat.Pdf, completed, total, job.PdfStatus.ToString());
        }

        Report(
            progress,
            job,
            job.Status == ConversionStatus.Success ? ConversionStage.Completed : ConversionStage.Failed,
            format: null,
            completed,
            total,
            job.ErrorMessage ?? "转换完成。");

        _logger?.Info($"转换结束：{request.InputPath}，状态 {job.Status}。");

        return job.ToResult();
    }

    private static int CountFormats(OutputFormat formats)
    {
        var count = 0;

        if (formats.HasFlag(OutputFormat.Docx))
        {
            count++;
        }

        if (formats.HasFlag(OutputFormat.Pdf))
        {
            count++;
        }

        return count;
    }

    private static void Report(
        IProgress<ConversionProgress>? progress,
        ConversionJob job,
        ConversionStage stage,
        OutputFormat? format,
        int completed,
        int total,
        string message)
    {
        progress?.Report(new ConversionProgress(
            job.Id,
            job.InputPath,
            stage,
            format,
            completed,
            total,
            job.Status,
            message));
    }
}
