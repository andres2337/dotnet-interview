using TodoApi.Dtos.Sync;
using TodoApi.Enums;

namespace TodoApi.Services.Sync;

public interface ITodoSyncService
{
    Task<SyncRunResponse> RunSyncAsync(SyncTriggerType triggerType, CancellationToken cancellationToken = default);
    Task<SyncStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
}
