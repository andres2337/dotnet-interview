namespace TodoApi.Services.ExternalApi;

public class ExternalUpdateTodoItemRequest
{
    public required string Text { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsDeleted { get; set; }
}
