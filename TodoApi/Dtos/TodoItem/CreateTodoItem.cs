using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.TodoItem;

public class CreateTodoItem
{
    [Required(ErrorMessage = "The item text is required.")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "The text must be between 1 and 500 characters.")]
    public required string Text { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "A valid TodoListId must be specified.")]
    public long TodoListId { get; set; }
}