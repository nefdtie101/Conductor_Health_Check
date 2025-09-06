namespace Conductor_Health_Check.Services;

public class LogService
{
    private readonly string _logFilePath;
    private readonly long _maxSizeBytes;
    private readonly object _lockObject = new object();
    private readonly IConfiguration _configuration;

    public LogService(IConfiguration configuration)
    {
        _configuration = configuration;
        _logFilePath = _configuration["LogFilePath"] ?? "conductor_health.log";
        _maxSizeBytes = 20 * 1024 * 1024; // 20MB in bytes
    }

    public void Log(string message)
    {
        lock (_lockObject)
        {
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
            
            // Check file size and rotate if needed
            if (File.Exists(_logFilePath) && new FileInfo(_logFilePath).Length > _maxSizeBytes)
            {
                RotateLogFile();
            }
            
            File.AppendAllText(_logFilePath, logEntry);
            Console.WriteLine(message); // Keep console logging too
        }
    }

    private void RotateLogFile()
    {
        // Clear the file when it gets too big
        File.WriteAllText(_logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Log file rotated{Environment.NewLine}");
    }

    public string GetLogs()
    {
        lock (_lockObject)
        {
            if (!File.Exists(_logFilePath))
            {
                return "No logs available";
            }
            return File.ReadAllText(_logFilePath);
        }
    }

    public async Task<string> GetLogsAsync()
    {
        return await Task.Run(() => GetLogs());
    }
}
