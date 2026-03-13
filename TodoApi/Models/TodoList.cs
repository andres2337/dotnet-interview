using System.ComponentModel.DataAnnotations;
using TodoApi.Enums;

namespace TodoApi.Models;

public class TodoList
{
    public long Id { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Name { get; set; }

    public bool IsDeleted { get; set; }
    public long? ExternalId { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.PendingCreate;
    public DateTime LastModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAtUtc { get; set; }
    [StringLength(1000)]
    public string? LastSyncError { get; set; }

    public ICollection<TodoItem> Items { get; set; } = [];
}
