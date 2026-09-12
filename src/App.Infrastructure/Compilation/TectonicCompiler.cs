using System.ComponentModel;
using System.Diagnostics;
using App.Application;

namespace App.Infrastructure;

public sealed class TectonicCompiler(
    TimeSpan timeout,
    int maxTexBytes,
    string executable = "tectonic") : ITexCompiler
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

            using var process = StartCompiler(workingDirectory, inputPath, executable);
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
                throw new InvalidOperationException("The TeX compiler completed without producing a PDF.");
            }

            return await File.ReadAllBytesAsync(outputPath, cancellationToken);
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException(
                $"TeX compiler '{Path.GetFileName(executable)}' is not installed or not on PATH.",
                exception);
        }
        finally
        {
            DeleteWorkingDirectory(workingDirectory);
        }
    }

    private static Process StartCompiler(
        string workingDirectory,
        string inputPath,
        string executable)
    {
        var engine = Path.GetFileName(executable);
        var isPdfLatex = engine.Equals("pdflatex", StringComparison.OrdinalIgnoreCase);
        var isTectonic = engine.Equals("tectonic", StringComparison.OrdinalIgnoreCase);
        if (!isPdfLatex && !isTectonic)
        {
            throw new InvalidOperationException(
                "Compilation:Executable must resolve to either 'tectonic' or 'pdflatex'.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (isPdfLatex)
        {
            // Kpathsea's paranoid mode rejects absolute and parent-relative file access.
            startInfo.Environment["openin_any"] = "p";
            startInfo.Environment["openout_any"] = "p";
            startInfo.Environment["TEXMFOUTPUT"] = workingDirectory;
            startInfo.ArgumentList.Add("-interaction=nonstopmode");
            startInfo.ArgumentList.Add("-halt-on-error");
            startInfo.ArgumentList.Add("-no-shell-escape");
            startInfo.ArgumentList.Add("-output-directory");
            startInfo.ArgumentList.Add(workingDirectory);
        }
        else
        {
            startInfo.ArgumentList.Add("-X");
            startInfo.ArgumentList.Add("compile");
            startInfo.ArgumentList.Add("--untrusted");
            startInfo.ArgumentList.Add("--only-cached");
            startInfo.ArgumentList.Add("--outdir");
            startInfo.ArgumentList.Add(workingDirectory);
        }
        startInfo.ArgumentList.Add(inputPath);

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start TeX compiler '{engine}'.");
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
