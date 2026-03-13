namespace TodoApi.Services.ExternalApi;

public class ExternalCreateTodoItemRequest
{
    public required string Text { get; set; }
    public bool IsCompleted { get; set; }
}
