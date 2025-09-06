namespace PowerShell;

using System;
using System.Diagnostics;
using System.Text;

public class PowershellRunner
{
    public static string ExecuteCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command must be a non-empty string.", nameof(command));

        var fileName = OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh";
        var args = OperatingSystem.IsWindows()
            ? $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{command}\""
            : $"-NoLogo -NoProfile -NonInteractive -Command \"{command}\"";

        var processInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
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
            throw new InvalidOperationException($"PowerShell command failed with exit code {process.ExitCode}.{(string.IsNullOrWhiteSpace(err) ? "" : $" Error: {err}")}");
        }

        return output.ToString().TrimEnd();
    }
}