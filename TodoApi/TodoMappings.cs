using TodoApi.Dtos.Sync;
using TodoApi.Dtos.TodoItem;
using TodoApi.Dtos.TodoList;
using TodoApi.Models;

namespace TodoApi;

public static class TodoMappings
{
    public static TodoListResponse ToResponse(this TodoList todoList, bool includeItems = true)
    {
        return new TodoListResponse
        {
            Id = todoList.Id,
            Name = todoList.Name,
            IsDeleted = todoList.IsDeleted,
            Sync = todoList.ToSyncResponse(),
            Items = includeItems
                ? todoList.Items.OrderBy(x => x.Id).Select(x => x.ToResponse()).ToList()
                : []
        };
    }

    public static TodoItemResponse ToResponse(this TodoItem todoItem)
    {
        return new TodoItemResponse
        {
            Id = todoItem.Id,
            Text = todoItem.Text,
            IsCompleted = todoItem.IsCompleted,
            IsDeleted = todoItem.IsDeleted,
            TodoListId = todoItem.TodoListId,
            Sync = todoItem.ToSyncResponse(),
        };
    }

    public static SyncMetadataResponse ToSyncResponse(this TodoList todoList)
    {
        return new SyncMetadataResponse
        {
            SyncStatus = todoList.SyncStatus,
            LastModifiedAtUtc = todoList.LastModifiedAtUtc,
            LastSyncedAtUtc = todoList.LastSyncedAtUtc,
            LastSyncError = todoList.LastSyncError,
            ExternalId = todoList.ExternalId,
        };
    }

    public static SyncMetadataResponse ToSyncResponse(this TodoItem todoItem)
    {
        return new SyncMetadataResponse
        {
            SyncStatus = todoItem.SyncStatus,
            LastModifiedAtUtc = todoItem.LastModifiedAtUtc,
            LastSyncedAtUtc = todoItem.LastSyncedAtUtc,
            LastSyncError = todoItem.LastSyncError,
            ExternalId = todoItem.ExternalId,
        };
    }
}
