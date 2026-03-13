namespace ExternalTodoApi.Mock.Models;

public class UpdateTodoItemBody
{
    public required string Text { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsDeleted { get; set; }
}
