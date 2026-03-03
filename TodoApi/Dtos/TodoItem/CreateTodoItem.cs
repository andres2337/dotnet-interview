namespace TodoApi.Dtos.TodoItem;

public class CreateTodoItem
{
    public required string Text { get; set; }
    public long TodoListId { get; set; }
}