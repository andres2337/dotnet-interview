using Microsoft.AspNetCore.Mvc;
using TodoApi.Enums;
using TodoApi.Services.Sync;

namespace TodoApi.Controllers;

[Route("api/sync")]
[ApiController]
public class SyncController : ControllerBase
{
    private readonly ITodoSyncService _todoSyncService;

    public SyncController(ITodoSyncService todoSyncService)
    {
        _todoSyncService = todoSyncService;
    }

    [HttpPost("run")]
    public async Task<ActionResult> Run(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _todoSyncService.RunSyncAsync(SyncTriggerType.Manual, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult> GetStatus(CancellationToken cancellationToken)
    {
        return Ok(await _todoSyncService.GetStatusAsync(cancellationToken));
    }
}
