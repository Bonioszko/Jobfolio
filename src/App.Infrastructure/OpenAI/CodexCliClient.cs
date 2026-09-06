using System.ComponentModel;
using System.Diagnostics;

namespace App.Infrastructure;

internal interface ICodexCliClient
{
    Task<string> ExecuteAsync(
        string prompt,
        string outputSchemaJson,
        CancellationToken cancellationToken);
}

internal sealed class CodexCliClient(CodexCliOptions options) : ICodexCliClient
{
    public async Task<string> ExecuteAsync(
        string prompt,
        string outputSchemaJson,
        CancellationToken cancellationToken)
    {
        var workingDirectory = Path.Combine(
            Path.GetTempPath(),
            "job-parser-codex",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);
        var schemaPath = Path.Combine(workingDirectory, "cv-output.schema.json");

        try
        {
            await File.WriteAllTextAsync(schemaPath, outputSchemaJson, cancellationToken);
            using var process = new Process
            {
                StartInfo = CreateStartInfo(workingDirectory, schemaPath)
            };
            using var timeout = new CancellationTokenSource(options.Timeout);
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeout.Token);

            try
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException("The local Codex process did not start.");
                }

                var stdout = process.StandardOutput.ReadToEndAsync(linkedCancellation.Token);
                var stderr = process.StandardError.ReadToEndAsync(linkedCancellation.Token);
                await process.StandardInput.WriteAsync(prompt.AsMemory(), linkedCancellation.Token);
                process.StandardInput.Close();
                await process.WaitForExitAsync(linkedCancellation.Token);

                var output = await stdout;
                _ = await stderr;
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Local Codex CV generation failed with exit code {process.ExitCode}.");
                }

                if (output.Length > options.MaxOutputCharacters)
                {
                    throw new FormatException("Local Codex returned an oversized response.");
                }

                return output;
            }
            catch (OperationCanceledException) when (
                timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                TryTerminate(process);
                throw new InvalidOperationException("Local Codex CV generation timed out.");
            }
            catch (Win32Exception exception)
            {
                throw new InvalidOperationException(
                    "The local Codex CLI could not be started. Install it or configure CodexCli:ExecutablePath.",
                    exception);
            }
            finally
            {
                TryTerminate(process);
            }
        }
        finally
        {
            try
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup; the directory contains only the non-sensitive JSON schema.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup; the directory contains only the non-sensitive JSON schema.
            }
        }
    }

    private ProcessStartInfo CreateStartInfo(string workingDirectory, string schemaPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = options.ExecutablePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--ephemeral");
        startInfo.ArgumentList.Add("--sandbox");
        startInfo.ArgumentList.Add("read-only");
        startInfo.ArgumentList.Add("--skip-git-repo-check");
        startInfo.ArgumentList.Add("--ignore-user-config");
        startInfo.ArgumentList.Add("--color");
        startInfo.ArgumentList.Add("never");
        if (options.Model is not null)
        {
            startInfo.ArgumentList.Add("--model");
            startInfo.ArgumentList.Add(options.Model);
        }

        startInfo.ArgumentList.Add("--output-schema");
        startInfo.ArgumentList.Add(schemaPath);
        startInfo.ArgumentList.Add("-");
        return startInfo;
    }

    private static void TryTerminate(Process process)
    {
        try
        {
            if (process.StartTime != default && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process was never started or exited between checks.
        }
        catch (Win32Exception)
        {
            // The process already exited or could not be terminated.
        }
    }
}
