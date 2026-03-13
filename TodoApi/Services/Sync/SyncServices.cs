using System.Net;
using Microsoft.EntityFrameworkCore;
using TodoApi.Enums;
using TodoApi.Models;
using TodoApi.Services.ExternalApi;

namespace TodoApi.Services.Sync;

public class SyncPlanner : ISyncPlanner
{
    public async Task<SyncPlan> BuildPlanAsync(IReadOnlyList<ExternalTodoListDto> externalLists, TodoContext context, CancellationToken cancellationToken)
    {
        var plan = new SyncPlan();

        var localLists = await context.TodoList
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);

        var localByExternalId = localLists
            .Where(x => x.ExternalId.HasValue)
            .GroupBy(x => x.ExternalId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(x => x.LastModifiedAtUtc)
                    .ThenByDescending(x => x.Id)
                    .First());

        foreach (var externalList in externalLists)
        {
            if (!localByExternalId.ContainsKey(externalList.Id))
            {
                plan.ExternalListsToImport.Add(externalList);
                continue;
            }

            plan.ExternalListsToMerge.Add(externalList);
        }

        plan.LocalListsPending.AddRange(localLists.Where(x => x.SyncStatus is SyncStatus.PendingCreate or SyncStatus.PendingUpdate or SyncStatus.PendingDelete or SyncStatus.Failed));
        plan.LocalItemsPending.AddRange(localLists.SelectMany(x => x.Items).Where(x => x.SyncStatus is SyncStatus.PendingCreate or SyncStatus.PendingUpdate or SyncStatus.PendingDelete or SyncStatus.Failed));

        return plan;
    }
}

public class SyncExecutor : ISyncExecutor
{
    private readonly TodoContext _context;
    private readonly IExternalTodoApiClient _externalClient;
    private readonly ILogger<SyncExecutor> _logger;

    public SyncExecutor(TodoContext context, IExternalTodoApiClient externalClient, ILogger<SyncExecutor> logger)
    {
        _context = context;
        _externalClient = externalClient;
        _logger = logger;
    }

    public async Task<SyncExecutionResult> ExecuteAsync(SyncPlan plan, CancellationToken cancellationToken)
    {
        var result = new SyncExecutionResult();
        await ImportRemoteListsAsync(plan.ExternalListsToImport, result, cancellationToken);
        await MergeRemoteListsAsync(plan.ExternalListsToMerge, result, cancellationToken);
        await PushLocalListsAsync(plan.LocalListsPending, result, cancellationToken);
        await PushLocalItemsAsync(plan.LocalItemsPending, result, cancellationToken);
        return result;
    }

    private async Task ImportRemoteListsAsync(IEnumerable<ExternalTodoListDto> externalLists, SyncExecutionResult result, CancellationToken cancellationToken)
    {
        foreach (var externalList in externalLists)
        {
            var newList = new TodoList
            {
                Name = externalList.Name,
                IsDeleted = externalList.IsDeleted,
                ExternalId = externalList.Id,
                SyncStatus = SyncStatus.Synced,
                LastModifiedAtUtc = DateTime.UtcNow,
                LastSyncedAtUtc = DateTime.UtcNow,
                Items = externalList.TodoItems.Select(item => new TodoItem
                {
                    Text = item.Text,
                    IsCompleted = item.IsCompleted,
                    IsDeleted = item.IsDeleted,
                    ExternalId = item.Id,
                    SyncStatus = SyncStatus.Synced,
                    LastModifiedAtUtc = DateTime.UtcNow,
                    LastSyncedAtUtc = DateTime.UtcNow,
                }).ToList()
            };

            _context.TodoList.Add(newList);
            result.CreatedCount += 1 + newList.Items.Count;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task MergeRemoteListsAsync(IEnumerable<ExternalTodoListDto> externalLists, SyncExecutionResult result, CancellationToken cancellationToken)
    {
        foreach (var externalList in externalLists)
        {
            var localList = await _context.TodoList.Include(x => x.Items).FirstAsync(x => x.ExternalId == externalList.Id, cancellationToken);
            if (localList.SyncStatus is SyncStatus.PendingCreate or SyncStatus.PendingUpdate or SyncStatus.PendingDelete)
            {
                result.SkippedCount++;
                continue;
            }

            localList.Name = externalList.Name;
            localList.IsDeleted = externalList.IsDeleted;
            localList.SyncStatus = SyncStatus.Synced;
            localList.LastSyncedAtUtc = DateTime.UtcNow;
            localList.LastSyncError = null;

            var localItemsByExternalId = localList.Items
                .Where(x => x.ExternalId.HasValue)
                .GroupBy(x => x.ExternalId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(x => x.LastModifiedAtUtc)
                        .ThenByDescending(x => x.Id)
                        .First());

            foreach (var externalItem in externalList.TodoItems)
            {
                if (!localItemsByExternalId.TryGetValue(externalItem.Id, out var localItem))
                {
                    localList.Items.Add(new TodoItem
                    {
                        Text = externalItem.Text,
                        IsCompleted = externalItem.IsCompleted,
                        IsDeleted = externalItem.IsDeleted,
                        ExternalId = externalItem.Id,
                        SyncStatus = SyncStatus.Synced,
                        LastModifiedAtUtc = DateTime.UtcNow,
                        LastSyncedAtUtc = DateTime.UtcNow,
                    });
                    result.CreatedCount++;
                    continue;
                }

                if (localItem.SyncStatus is SyncStatus.PendingCreate or SyncStatus.PendingUpdate or SyncStatus.PendingDelete)
                {
                    result.SkippedCount++;
                    continue;
                }

                localItem.Text = externalItem.Text;
                localItem.IsCompleted = externalItem.IsCompleted;
                localItem.IsDeleted = externalItem.IsDeleted;
                localItem.SyncStatus = SyncStatus.Synced;
                localItem.LastSyncedAtUtc = DateTime.UtcNow;
                localItem.LastSyncError = null;
                result.UpdatedCount++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task PushLocalListsAsync(IEnumerable<TodoList> localLists, SyncExecutionResult result, CancellationToken cancellationToken)
    {
        foreach (var localList in localLists.OrderBy(x => x.Id))
        {
            try
            {
                if (localList.SyncStatus == SyncStatus.PendingDelete && localList.ExternalId.HasValue)
                {
                    await _externalClient.DeleteTodoListAsync(localList.ExternalId.Value, cancellationToken);
                    MarkSynced(localList);
                    result.DeletedCount++;
                    continue;
                }

                if (!localList.ExternalId.HasValue)
                {
                    await CreateRemoteListAsync(localList, result, cancellationToken);
                    continue;
                }

                var pendingNewItems = localList.Items
                    .Where(x => !x.IsDeleted && !x.ExternalId.HasValue)
                    .ToList();

                await _externalClient.UpdateTodoListAsync(localList.ExternalId.Value, new ExternalUpdateTodoListRequest
                {
                    Name = localList.Name,
                    IsDeleted = localList.IsDeleted,
                    TodoItems = pendingNewItems
                        .Select(x => new ExternalCreateTodoItemRequest { Text = x.Text, IsCompleted = x.IsCompleted })
                        .ToList()
                }, cancellationToken);

                if (pendingNewItems.Count > 0)
                {
                    var externalLists = await _externalClient.GetTodoListsAsync(cancellationToken);
                    var refreshedExternalList = externalLists.FirstOrDefault(x => x.Id == localList.ExternalId.Value);
                    if (refreshedExternalList is not null)
                    {
                        ReconcileCreatedItems(localList, refreshedExternalList.TodoItems);
                    }
                }

                MarkSynced(localList);
                result.UpdatedCount++;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                localList.ExternalId = null;
                localList.SyncStatus = SyncStatus.PendingCreate;
                localList.LastSyncError = "Remote list reference was missing. Recreating list in the external API.";
                ResetItemsForRecreate(localList);

                await CreateRemoteListAsync(localList, result, cancellationToken);
            }
            catch (Exception ex)
            {
                MarkFailed(localList, ex.Message);
                result.FailedCount++;
                result.Errors.Add($"TodoList {localList.Id}: {ex.Message}");
                _logger.LogError(ex, "Failed to synchronize local todo list {TodoListId}", localList.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task PushLocalItemsAsync(IEnumerable<TodoItem> localItems, SyncExecutionResult result, CancellationToken cancellationToken)
    {
        foreach (var localItem in localItems.OrderBy(x => x.Id))
        {
            if (localItem.IsDeleted && !localItem.ExternalId.HasValue)
            {
                MarkSynced(localItem);
                result.SkippedCount++;
                continue;
            }

            if (!localItem.ExternalId.HasValue && localItem.TodoList.ExternalId.HasValue)
            {
                result.SkippedCount++;
                localItem.LastSyncError = "Waiting for list-level sync to assign an external item id.";
                continue;
            }

            if (!localItem.ExternalId.HasValue || !localItem.TodoList.ExternalId.HasValue)
            {
                continue;
            }

            try
            {
                if (localItem.SyncStatus == SyncStatus.PendingDelete)
                {
                    await _externalClient.DeleteTodoItemAsync(localItem.TodoList.ExternalId.Value, localItem.ExternalId.Value, cancellationToken);
                    MarkSynced(localItem);
                    result.DeletedCount++;
                    continue;
                }

                await _externalClient.UpdateTodoItemAsync(localItem.TodoList.ExternalId.Value, localItem.ExternalId.Value, new ExternalUpdateTodoItemRequest
                {
                    Text = localItem.Text,
                    IsCompleted = localItem.IsCompleted,
                    IsDeleted = localItem.IsDeleted
                }, cancellationToken);

                MarkSynced(localItem);
                result.UpdatedCount++;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                localItem.ExternalId = null;
                localItem.SyncStatus = SyncStatus.PendingCreate;
                localItem.LastSyncError = "Remote item reference was missing. Waiting for list-level sync to recreate it.";
                localItem.TodoList.SyncStatus = SyncStatus.PendingUpdate;
                localItem.TodoList.LastModifiedAtUtc = DateTime.UtcNow;
                result.SkippedCount++;
            }
            catch (Exception ex)
            {
                MarkFailed(localItem, ex.Message);
                result.FailedCount++;
                result.Errors.Add($"TodoItem {localItem.Id}: {ex.Message}");
                _logger.LogError(ex, "Failed to synchronize local todo item {TodoItemId}", localItem.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateRemoteListAsync(TodoList localList, SyncExecutionResult result, CancellationToken cancellationToken)
    {
        var created = await _externalClient.CreateTodoListAsync(new ExternalCreateTodoListRequest
        {
            Name = localList.Name,
            TodoItems = localList.Items
                .Where(x => !x.IsDeleted)
                .Select(x => new ExternalCreateTodoItemRequest { Text = x.Text, IsCompleted = x.IsCompleted })
                .ToList()
        }, cancellationToken);

        localList.ExternalId = created.Id;
        MarkSynced(localList);
        ReconcileCreatedItems(localList, created.TodoItems);
        result.CreatedCount++;
    }

    private static void ResetItemsForRecreate(TodoList localList)
    {
        foreach (var item in localList.Items.Where(x => !x.IsDeleted))
        {
            item.ExternalId = null;
            item.SyncStatus = SyncStatus.PendingCreate;
            item.LastSyncError = null;
        }
    }

    private static void ReconcileCreatedItems(TodoList localList, IEnumerable<ExternalTodoItemDto> externalItems)
    {
        var remaining = externalItems.ToList();

        foreach (var localItem in localList.Items.Where(x => !x.IsDeleted && !x.ExternalId.HasValue).OrderBy(x => x.Id))
        {
            var matched = remaining.FirstOrDefault(x => x.Text == localItem.Text && x.IsCompleted == localItem.IsCompleted && !x.IsDeleted);
            if (matched is null)
            {
                continue;
            }

            localItem.ExternalId = matched.Id;
            MarkSynced(localItem);
            remaining.Remove(matched);
        }
    }

    private static void MarkSynced(TodoList todoList)
    {
        todoList.SyncStatus = SyncStatus.Synced;
        todoList.LastSyncedAtUtc = DateTime.UtcNow;
        todoList.LastSyncError = null;
    }

    private static void MarkSynced(TodoItem todoItem)
    {
        todoItem.SyncStatus = SyncStatus.Synced;
        todoItem.LastSyncedAtUtc = DateTime.UtcNow;
        todoItem.LastSyncError = null;
    }

    private static void MarkFailed(TodoList todoList, string error)
    {
        todoList.SyncStatus = SyncStatus.Failed;
        todoList.LastSyncError = error;
    }

    private static void MarkFailed(TodoItem todoItem, string error)
    {
        todoItem.SyncStatus = SyncStatus.Failed;
        todoItem.LastSyncError = error;
    }
}


