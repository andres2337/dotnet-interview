using TodoApi.Enums;

namespace TodoApi.Dtos.Sync;

public class SyncMetadataResponse
{
    public SyncStatus SyncStatus { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
    public DateTime? LastSyncedAtUtc { get; set; }
    public string? LastSyncError { get; set; }
    public long? ExternalId { get; set; }
}
