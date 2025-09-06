using Bash;
using PowerShell;

namespace Conductor_Health_Check;

public class TaskService : IHostedService, IDisposable
{
    private Timer _timer;
    private readonly IConfiguration _configuration;
    
    private BashRunner _bashRunner;
    private PowershellRunner _powershellRunner;
    
    public TaskService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Run immediately on start
        DoWork(null);
        
        var now = DateTime.Now;
    
        // Align first run to the next 20-minute boundary (e.g., :00, :20, :40)
        var minutesToNextSlot = (20 - (now.Minute % 20)) % 20;
        var nextRun = now.AddMinutes(minutesToNextSlot)
            .AddSeconds(-now.Second)
            .AddMilliseconds(-now.Millisecond);
    
        var initialDelay = nextRun - now;
    
        _timer = new Timer(DoWork, null, initialDelay, TimeSpan.FromMinutes(20));
        return Task.CompletedTask;
    }
    
    
private void DoWork(object state)
    {
        var gatewayIP = _configuration["GatewayIP"];
        
        try
        {
            string result;
            if (OperatingSystem.IsWindows())
            {
                // Use PowerShell on Windows
                result = PowershellRunner.ExecuteCommand($"Test-Connection -ComputerName {gatewayIP} -Count 4 -Quiet");
                Console.WriteLine($"VPN Gateway ping status: {result}");
            }
            else
            {
                // Use Bash on Linux/macOS
                result = BashRunner.ExecuteCommand($"ping -c 4 {gatewayIP}");
                Console.WriteLine($"VPN Gateway ping result: {result}");
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"VPN connection check failed: {ex.Message}");
            // Implement any notification or recovery logic here
        }
    }
    
   

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
    
}