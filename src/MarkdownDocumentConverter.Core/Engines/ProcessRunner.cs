using System.Diagnostics;
using MarkdownDocumentConverter.Core.Exceptions;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Engines;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(arguments);

        if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "超时必须大于零。");
        }

        if (!File.Exists(fileName))
        {
            throw new ProcessExecutionException($"找不到可执行文件：{fileName}", fileName);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory ?? string.Empty,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
        {
            ArgumentNullException.ThrowIfNull(argument);
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!process.Start())
            {
                throw new ProcessExecutionException($"无法启动进程：{Path.GetFileName(fileName)}", fileName);
            }
        }
        catch (ProcessExecutionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ProcessExecutionException(
                $"无法启动进程：{Path.GetFileName(fileName)}。{ex.Message}",
                fileName,
                ex);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stderrTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

        using var timeoutSource = new CancellationTokenSource();
        if (timeout != Timeout.InfiniteTimeSpan)
        {
            timeoutSource.CancelAfter(timeout);
        }

        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (linkedSource.IsCancellationRequested)
        {
            TryKillProcessTree(process);

            var cancelledStdout = await SafeReadAsync(stdoutTask).ConfigureAwait(false);
            var cancelledStderr = await SafeReadAsync(stderrTask).ConfigureAwait(false);
            stopwatch.Stop();

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("操作已取消。", cancellationToken);
            }

            throw new ProcessExecutionException(
                $"进程运行超时（{timeout.TotalSeconds:0} 秒）：{Path.GetFileName(fileName)}",
                fileName,
                cancelledStdout,
                cancelledStderr,
                null,
                timedOut: true,
                stopwatch.Elapsed);
        }

        var standardOutput = await stdoutTask.ConfigureAwait(false);
        var standardError = await stderrTask.ConfigureAwait(false);
        stopwatch.Stop();

        if (process.ExitCode != 0)
        {
            throw new ProcessExecutionException(
                $"进程返回错误退出码 {process.ExitCode}：{Path.GetFileName(fileName)}。{Summarize(standardError)}",
                fileName,
                standardOutput,
                standardError,
                process.ExitCode,
                timedOut: false,
                stopwatch.Elapsed);
        }

        return new ProcessResult(
            process.ExitCode,
            standardOutput,
            standardError,
            stopwatch.Elapsed);
    }

    private static async Task<string> SafeReadAsync(Task<string> readTask)
    {
        try
        {
            return await readTask.ConfigureAwait(false);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // 进程已经退出。
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // 尽力终止；原始错误仍会通过调用方报告。
        }
    }

    private static string Summarize(string text)
    {
        const int maxLength = 400;
        var normalized = text.ReplaceLineEndings(" ").Trim();

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength] + "…";
    }
}
