using System;
using System.Diagnostics;
using System.Text;

namespace Bash;

public class BashRunner
{
    public static string ExecuteCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command must be a non-empty string.", nameof(command));

        var processInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, data) =>
        {
            if (data.Data != null) output.AppendLine(data.Data);
        };
        process.ErrorDataReceived += (sender, data) =>
        {
            if (data.Data != null) error.AppendLine(data.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var err = error.ToString();
            throw new InvalidOperationException($"Bash command failed with exit code {process.ExitCode}.{(string.IsNullOrWhiteSpace(err) ? "" : $" Error: {err}")}");
        }

        return output.ToString().TrimEnd();
    }
}