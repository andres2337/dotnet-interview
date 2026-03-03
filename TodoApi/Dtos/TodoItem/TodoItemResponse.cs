namespace TodoApi.Dtos.TodoItem;

public class TodoItemResponse
{
    public long Id { get; set; }
    public bool IsCompleted { get; set; }
    public required string Text { get; set; }
}