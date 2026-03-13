namespace TodoApi.Services.ExternalApi;

public class ExternalTodoItemDto
{
    public long Id { get; set; }
    public required string Text { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsDeleted { get; set; }
}
