using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

public class TodoList
{
    public long Id { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Name { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<TodoItem> Items { get; set; } = [];
}
