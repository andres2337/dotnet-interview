using System.Threading;
using Microsoft.EntityFrameworkCore;
using TodoApi.Dtos.Sync;
using TodoApi.Enums;
using TodoApi.Models;
using TodoApi.Services.ExternalApi;

namespace TodoApi.Services.Sync;

public class TodoSyncService : ITodoSyncService
{
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    private readonly TodoContext _context;
    private readonly IExternalTodoApiClient _externalClient;
    private readonly ISyncPlanner _planner;
    private readonly ISyncExecutor _executor;
    private readonly ILogger<TodoSyncService> _logger;

    public TodoSyncService(TodoContext context, IExternalTodoApiClient externalClient, ISyncPlanner planner, ISyncExecutor executor, ILogger<TodoSyncService> logger)
    {
        _context = context;
        _externalClient = externalClient;
        _planner = planner;
        _executor = executor;
        _logger = logger;
    }

    public async Task<SyncRunResponse> RunSyncAsync(SyncTriggerType triggerType, CancellationToken cancellationToken = default)
    {
        if (!await SyncLock.WaitAsync(0, cancellationToken))
        {
            throw new InvalidOperationException("A synchronization run is already in progress.");
        }

        var syncRun = new SyncRun
        {
            StartedAtUtc = DateTime.UtcNow,
            Status = SyncRunStatus.Running,
            TriggeredBy = triggerType,
        };

        _context.SyncRun.Add(syncRun);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Starting synchronization run {SyncRunId} triggered by {TriggerType}", syncRun.Id, triggerType);
            var externalLists = await _externalClient.GetTodoListsAsync(cancellationToken);
            var plan = await _planner.BuildPlanAsync(externalLists, _context, cancellationToken);
            var result = await _executor.ExecuteAsync(plan, cancellationToken);

            syncRun.CreatedCount = result.CreatedCount;
            syncRun.UpdatedCount = result.UpdatedCount;
            syncRun.DeletedCount = result.DeletedCount;
            syncRun.FailedCount = result.FailedCount;
            syncRun.SkippedCount = result.SkippedCount;
            syncRun.ErrorSummary = result.Errors.Count == 0 ? null : string.Join(" | ", result.Errors);
            syncRun.Status = result.FailedCount == 0 ? SyncRunStatus.Succeeded : (result.CreatedCount + result.UpdatedCount + result.DeletedCount) > 0 ? SyncRunStatus.PartialSuccess : SyncRunStatus.Failed;
            syncRun.FinishedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Synchronization run {SyncRunId} finished with status {Status}", syncRun.Id, syncRun.Status);
            return MapRun(syncRun);
        }
        catch (Exception ex)
        {
            syncRun.Status = SyncRunStatus.Failed;
            syncRun.FinishedAtUtc = DateTime.UtcNow;
            syncRun.ErrorSummary = ex.Message;
            syncRun.FailedCount += 1;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Synchronization run {SyncRunId} failed", syncRun.Id);
            throw;
        }
        finally
        {
            SyncLock.Release();
        }
    }

    public async Task<SyncStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var lastRun = await _context.SyncRun
            .OrderByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new SyncStatusResponse
        {
            LastRun = lastRun is null ? null : MapRun(lastRun),
            PendingListCount = await _context.TodoList.CountAsync(x => x.SyncStatus == SyncStatus.PendingCreate || x.SyncStatus == SyncStatus.PendingUpdate, cancellationToken),
            PendingItemCount = await _context.TodoItem.CountAsync(x => x.SyncStatus == SyncStatus.PendingCreate || x.SyncStatus == SyncStatus.PendingUpdate, cancellationToken),
            FailedListCount = await _context.TodoList.CountAsync(x => x.SyncStatus == SyncStatus.Failed, cancellationToken),
            FailedItemCount = await _context.TodoItem.CountAsync(x => x.SyncStatus == SyncStatus.Failed, cancellationToken),
        };
    }

    private static SyncRunResponse MapRun(SyncRun syncRun) => new()
    {
        Id = syncRun.Id,
        StartedAtUtc = syncRun.StartedAtUtc,
        FinishedAtUtc = syncRun.FinishedAtUtc,
        Status = syncRun.Status,
        TriggeredBy = syncRun.TriggeredBy,
        CreatedCount = syncRun.CreatedCount,
        UpdatedCount = syncRun.UpdatedCount,
        DeletedCount = syncRun.DeletedCount,
        FailedCount = syncRun.FailedCount,
        SkippedCount = syncRun.SkippedCount,
        ErrorSummary = syncRun.ErrorSummary,
    };
}
