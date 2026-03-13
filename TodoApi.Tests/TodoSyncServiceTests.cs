using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TodoApi.Enums;
using TodoApi.Models;
using TodoApi.Services.ExternalApi;
using TodoApi.Services.Sync;

namespace TodoApi.Tests;

public class TodoSyncServiceTests
{
    private static TodoContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TodoContext(options);
    }

    [Fact]
    public async Task RunSyncAsync_WhenExternalHasNewList_ImportsItAndCreatesSyncRun()
    {
        using var context = CreateContext();
        var externalClient = new FakeExternalTodoApiClient
        {
            TodoLists =
            [
                new ExternalTodoListDto
                {
                    Id = 900,
                    Name = "Remote list",
                    TodoItems = [new ExternalTodoItemDto { Id = 901, Text = "Remote item", IsCompleted = false }]
                }
            ]
        };

        var service = new TodoSyncService(
            context,
            externalClient,
            new SyncPlanner(),
            new SyncExecutor(context, externalClient, NullLogger<SyncExecutor>.Instance),
            NullLogger<TodoSyncService>.Instance);

        var result = await service.RunSyncAsync(SyncTriggerType.Manual);

        Assert.Equal(SyncRunStatus.Succeeded, result.Status);
        Assert.Single(context.TodoList);
        Assert.Single(context.TodoItem);
        Assert.Equal(1, context.SyncRun.Count());
        Assert.Equal("Remote list", context.TodoList.Single().Name);
        Assert.Equal(SyncStatus.Synced, context.TodoList.Single().SyncStatus);
    }

    [Fact]
    public async Task RunSyncAsync_WhenLocalHasPendingCreate_PushesItToExternalAndMarksAsSynced()
    {
        using var context = CreateContext();
        context.TodoList.Add(new TodoList
        {
            Id = 1,
            Name = "Local list",
            SyncStatus = SyncStatus.PendingCreate,
            LastModifiedAtUtc = DateTime.UtcNow,
            Items =
            [
                new TodoItem
                {
                    Id = 2,
                    Text = "Local item",
                    SyncStatus = SyncStatus.PendingCreate,
                    LastModifiedAtUtc = DateTime.UtcNow,
                }
            ]
        });
        await context.SaveChangesAsync();

        var externalClient = new FakeExternalTodoApiClient
        {
            CreateTodoListResult = new ExternalTodoListDto
            {
                Id = 300,
                Name = "Local list",
                TodoItems = [new ExternalTodoItemDto { Id = 301, Text = "Local item", IsCompleted = false }]
            }
        };

        var service = new TodoSyncService(
            context,
            externalClient,
            new SyncPlanner(),
            new SyncExecutor(context, externalClient, NullLogger<SyncExecutor>.Instance),
            NullLogger<TodoSyncService>.Instance);

        var result = await service.RunSyncAsync(SyncTriggerType.Hangfire);
        var localList = await context.TodoList.Include(x => x.Items).SingleAsync();
        var localItem = localList.Items.Single();

        Assert.Equal(SyncRunStatus.Succeeded, result.Status);
        Assert.Equal(300, localList.ExternalId);
        Assert.Equal(SyncStatus.Synced, localList.SyncStatus);
        Assert.Equal(301, localItem.ExternalId);
        Assert.Equal(SyncStatus.Synced, localItem.SyncStatus);
        Assert.Equal(1, externalClient.CreateTodoListCallCount);
    }

    private sealed class FakeExternalTodoApiClient : IExternalTodoApiClient
    {
        public IReadOnlyList<ExternalTodoListDto> TodoLists { get; set; } = [];
        public ExternalTodoListDto? CreateTodoListResult { get; set; }
        public int CreateTodoListCallCount { get; private set; }

        public Task<IReadOnlyList<ExternalTodoListDto>> GetTodoListsAsync(CancellationToken cancellationToken)
            => Task.FromResult(TodoLists);

        public Task<ExternalTodoListDto> CreateTodoListAsync(ExternalCreateTodoListRequest request, CancellationToken cancellationToken)
        {
            CreateTodoListCallCount++;
            return Task.FromResult(CreateTodoListResult ?? new ExternalTodoListDto { Id = 1, Name = request.Name });
        }

        public Task UpdateTodoListAsync(long externalTodoListId, ExternalUpdateTodoListRequest request, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task DeleteTodoListAsync(long externalTodoListId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateTodoItemAsync(long externalTodoListId, long externalTodoItemId, ExternalUpdateTodoItemRequest request, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task DeleteTodoItemAsync(long externalTodoListId, long externalTodoItemId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
