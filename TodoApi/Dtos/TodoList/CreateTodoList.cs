using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.TodoList;

public class CreateTodoList
{
    [Required(ErrorMessage = "The list name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "The name must be between 1 and 200 characters.")]
    public required string Name { get; set; }
}
