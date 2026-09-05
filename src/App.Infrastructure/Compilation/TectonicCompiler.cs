using System.ComponentModel;
using System.Diagnostics;
using App.Application;

namespace App.Infrastructure;

public sealed class TectonicCompiler(TimeSpan timeout, int maxTexBytes) : ITexCompiler
{
    private const int MaximumErrorLength = 2_000;

    public async Task<byte[]> CompileAsync(string tex, CancellationToken cancellationToken)
    {
        TexSafety.Validate(tex, maxTexBytes);
        var workingDirectory = Path.Combine(
            Path.GetTempPath(),
            $"jobparser-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var inputPath = Path.Combine(workingDirectory, "main.tex");
            var outputPath = Path.Combine(workingDirectory, "main.pdf");
            await File.WriteAllTextAsync(inputPath, tex, cancellationToken);

            using var process = StartTectonic(workingDirectory, inputPath);
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException exception)
            {
                await TerminateAsync(process);
                if (cancellationToken.IsCancellationRequested) throw;

                throw new TexCompilationTimeoutException(
                    $"TeX compilation exceeded the {timeout.TotalSeconds:0}-second timeout.",
                    exception);
            }

            var output = await standardOutput;
            var error = await standardError;

            if (process.ExitCode != 0)
            {
                var safeError = string.IsNullOrWhiteSpace(error) ? output : error;
                throw new InvalidOperationException(Truncate(safeError));
            }

            if (!File.Exists(outputPath))
            {
                throw new InvalidOperationException("Tectonic completed without producing a PDF.");
            }

            return await File.ReadAllBytesAsync(outputPath, cancellationToken);
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException("Tectonic is not installed or not on PATH.", exception);
        }
        finally
        {
            DeleteWorkingDirectory(workingDirectory);
        }
    }

    private static Process StartTectonic(string workingDirectory, string inputPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "tectonic",
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-X");
        startInfo.ArgumentList.Add("compile");
        startInfo.ArgumentList.Add("--untrusted");
        startInfo.ArgumentList.Add("--only-cached");
        startInfo.ArgumentList.Add("--outdir");
        startInfo.ArgumentList.Add(workingDirectory);
        startInfo.ArgumentList.Add(inputPath);

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start Tectonic.");
    }

    private static async Task TerminateAsync(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // The process exited between the state check and termination.
        }
    }

    private static string Truncate(string message)
    {
        var trimmed = message.Trim();
        return trimmed[..Math.Min(MaximumErrorLength, trimmed.Length)];
    }

    private static void DeleteWorkingDirectory(string directory)
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
