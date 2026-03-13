using System.ComponentModel.DataAnnotations;

namespace ExternalTodoApi.Mock.Models;

public class CreateTodoListBody
{
    [Required]
    public required string Name { get; set; }
    public List<CreateTodoItemBody> TodoItems { get; set; } = [];
}
