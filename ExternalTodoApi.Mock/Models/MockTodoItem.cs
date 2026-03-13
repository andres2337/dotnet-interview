namespace ExternalTodoApi.Mock.Models;

public class MockTodoItem
{
    public long Id { get; set; }
    public required string Text { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsDeleted { get; set; }
}
