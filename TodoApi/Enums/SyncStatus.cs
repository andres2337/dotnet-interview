namespace TodoApi.Enums;

public enum SyncStatus
{
    PendingCreate = 1,
    PendingUpdate = 2,
    PendingDelete = 3,
    Synced = 4,
    Failed = 5
}
