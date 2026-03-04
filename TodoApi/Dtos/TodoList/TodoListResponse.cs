namespace TodoApi.Dtos.TodoList;

public class TodoListResponse
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public bool IsDeleted { get; set; }
}
