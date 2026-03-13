using TodoApi.Models;
using TodoApi.Services.ExternalApi;

namespace TodoApi.Services.Sync;

public class SyncPlan
{
    public List<ExternalTodoListDto> ExternalListsToImport { get; } = [];
    public List<ExternalTodoListDto> ExternalListsToMerge { get; } = [];
    public List<TodoList> LocalListsPending { get; } = [];
    public List<TodoItem> LocalItemsPending { get; } = [];
}
