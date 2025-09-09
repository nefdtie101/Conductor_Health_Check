using Bash;
using Broker;
using Conductor_Health_Check.Services;
using PowerShell;

namespace Conductor_Health_Check;

public class TaskService : IHostedService, IDisposable
{
    private Timer _timer;
    private readonly IConfiguration _configuration;
    private readonly LogService _logService;
    private readonly SmsBroker _smsBroker;
    public DateTime LastRun { get; set; }
    
    private BashRunner _bashRunner;
    private PowershellRunner _powershellRunner;
    
    public TaskService(IConfiguration configuration, LogService logService , SmsBroker smsBroker)
    {
        _configuration = configuration;
        _logService = logService;
        _smsBroker = smsBroker;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        DoWork(null);
        
        var now = DateTime.Now;
        
        var minutesToNextSlot = (20 - (now.Minute % 20)) % 20;
        var nextRun = now.AddMinutes(minutesToNextSlot)
            .AddSeconds(-now.Second)
            .AddMilliseconds(-now.Millisecond);
    
        var initialDelay = nextRun - now;
    
        _timer = new Timer(DoWork, null, initialDelay, TimeSpan.FromMinutes(20));
        return Task.CompletedTask;
    }
    
    
private async void DoWork(object? state)
    {
        var gatewayIP = _configuration["GatewayIP"];
        
        try
        {
            string result;
            if (OperatingSystem.IsWindows())
            {
                result = PowershellRunner.ExecuteCommand($"Test-Connection -ComputerName {gatewayIP} -Count 4 -Quiet");
                _logService.Log($"VPN Gateway ping status: {result}");
            }
            else
            {
                result = BashRunner.ExecuteCommand($"ping -c 4 {gatewayIP}");
                _logService.Log($"VPN Gateway ping result: {result}");
            }
        }
        catch (InvalidOperationException ex)
        {

            if (LastRun == DateTime.MinValue || DateTime.Now.Subtract(LastRun).TotalHours >= 24)
            {
                LastRun = DateTime.Now;
                var serverName = _configuration.GetSection("ServerName");
                var res =   await _smsBroker.sendSms($"VPN connection check failed on {serverName.Value}.");
                _logService.Log($"SMS sent {res}");
            }
            
            _logService.Log($"VPN connection check failed: {ex.Message}");
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