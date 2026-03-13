namespace TodoApi.Services.ExternalApi;

public class ExternalUpdateTodoListRequest
{
    public required string Name { get; set; }
    public bool IsDeleted { get; set; }
    public List<ExternalCreateTodoItemRequest>? TodoItems { get; set; }
}
