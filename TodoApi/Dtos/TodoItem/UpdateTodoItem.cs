using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.TodoItem;

public class UpdateTodoItem
{
    [Required(ErrorMessage = "The item text is required.")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "The text must be between 1 and 500 characters.")]
    public required string Text { get; set; }

    public bool IsCompleted { get; set; }
}