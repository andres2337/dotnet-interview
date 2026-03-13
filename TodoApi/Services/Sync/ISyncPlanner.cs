using TodoApi.Models;
using TodoApi.Services.ExternalApi;

namespace TodoApi.Services.Sync;

public interface ISyncPlanner
{
    Task<SyncPlan> BuildPlanAsync(IReadOnlyList<ExternalTodoListDto> externalLists, TodoContext context, CancellationToken cancellationToken);
}
