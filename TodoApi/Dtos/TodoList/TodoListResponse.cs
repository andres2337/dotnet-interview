using TodoApi.Dtos.Sync;
using TodoApi.Dtos.TodoItem;

namespace TodoApi.Dtos.TodoList;

public class TodoListResponse
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public bool IsDeleted { get; set; }
    public required SyncMetadataResponse Sync { get; set; }
    public IList<TodoItemResponse> Items { get; set; } = [];
}
