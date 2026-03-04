namespace TodoApi.Dtos.TodoItem;

public class UpdateTodoItem
{
    public required string Text { get; set; }
    public bool IsCompleted { get; set; }
}