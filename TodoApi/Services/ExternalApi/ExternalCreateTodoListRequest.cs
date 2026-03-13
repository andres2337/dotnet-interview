namespace TodoApi.Services.ExternalApi;

public class ExternalCreateTodoListRequest
{
    public required string Name { get; set; }
    public List<ExternalCreateTodoItemRequest> TodoItems { get; set; } = [];
}
