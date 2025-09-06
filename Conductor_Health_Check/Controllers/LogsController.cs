using Conductor_Health_Check.Services;
using Microsoft.AspNetCore.Mvc;

namespace Conductor_Health_Check.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly LogService _logService;

    public LogsController(LogService logService)
    {
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs()
    {
        try
        {
            var logs = await _logService.GetLogsAsync();
            return Ok(new { logs = logs, timestamp = DateTime.Now });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to retrieve logs", message = ex.Message });
        }
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadLogs()
    {
        try
        {
            var logs = await _logService.GetLogsAsync();
            var fileName = $"conductor_health_logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            
            return File(System.Text.Encoding.UTF8.GetBytes(logs), "text/plain", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to download logs", message = ex.Message });
        }
    }
}
