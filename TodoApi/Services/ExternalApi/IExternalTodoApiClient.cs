namespace TodoApi.Services.ExternalApi;

public interface IExternalTodoApiClient
{
    Task<IReadOnlyList<ExternalTodoListDto>> GetTodoListsAsync(CancellationToken cancellationToken);
    Task<ExternalTodoListDto> CreateTodoListAsync(ExternalCreateTodoListRequest request, CancellationToken cancellationToken);
    Task UpdateTodoListAsync(long externalTodoListId, ExternalUpdateTodoListRequest request, CancellationToken cancellationToken);
    Task DeleteTodoListAsync(long externalTodoListId, CancellationToken cancellationToken);
    Task UpdateTodoItemAsync(long externalTodoListId, long externalTodoItemId, ExternalUpdateTodoItemRequest request, CancellationToken cancellationToken);
    Task DeleteTodoItemAsync(long externalTodoListId, long externalTodoItemId, CancellationToken cancellationToken);
}
