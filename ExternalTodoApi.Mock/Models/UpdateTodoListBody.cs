namespace ExternalTodoApi.Mock.Models;

public class UpdateTodoListBody
{
    public required string Name { get; set; }
    public bool IsDeleted { get; set; }
    public List<CreateTodoItemBody>? TodoItems { get; set; }
}
