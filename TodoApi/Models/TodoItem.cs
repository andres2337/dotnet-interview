using System.ComponentModel.DataAnnotations;
using TodoApi.Enums;

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
    public long? ExternalId { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.PendingCreate;
    public DateTime LastModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAtUtc { get; set; }
    [StringLength(1000)]
    public string? LastSyncError { get; set; }
}
