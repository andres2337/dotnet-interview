namespace ExternalTodoApi.Mock.Models;

public class MockTodoList
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public bool IsDeleted { get; set; }
    public List<MockTodoItem> TodoItems { get; set; } = [];
}
