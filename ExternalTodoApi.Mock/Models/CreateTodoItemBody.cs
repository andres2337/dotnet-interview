namespace ExternalTodoApi.Mock.Models;

public class CreateTodoItemBody
{
    public required string Text { get; set; }
    public bool IsCompleted { get; set; }
}
