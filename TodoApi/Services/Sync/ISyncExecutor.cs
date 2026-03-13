namespace TodoApi.Services.Sync;

public interface ISyncExecutor
{
    Task<SyncExecutionResult> ExecuteAsync(SyncPlan plan, CancellationToken cancellationToken);
}
