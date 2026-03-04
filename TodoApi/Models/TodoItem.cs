namespace TodoApi.Models;

public class TodoItem
{
    public long Id { get; set; }
    public required string Text { get; set; }
    public bool IsCompleted { get; set; }
    public long TodoListId { get; set; }
    public bool IsDeleted { get; set; }
}
