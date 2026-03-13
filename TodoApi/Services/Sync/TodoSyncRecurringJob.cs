using Hangfire;
using TodoApi.Enums;

namespace TodoApi.Services.Sync;

public class TodoSyncRecurringJob
{
    private readonly ITodoSyncService _todoSyncService;

    public TodoSyncRecurringJob(ITodoSyncService todoSyncService)
    {
        _todoSyncService = todoSyncService;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public Task RunAsync()
    {
        return _todoSyncService.RunSyncAsync(SyncTriggerType.Hangfire);
    }
}
