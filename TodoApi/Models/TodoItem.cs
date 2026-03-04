using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

public class TodoItem
{
    public long Id { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Text { get; set; }

    public bool IsCompleted { get; set; }

    public long TodoListId { get; set; }
    public TodoList TodoList { get; set; } = null!;

    public bool IsDeleted { get; set; }
}
