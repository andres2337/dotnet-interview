namespace TodoApi.Dtos.Sync;

public class SyncStatusResponse
{
    public SyncRunResponse? LastRun { get; set; }
    public int PendingListCount { get; set; }
    public int PendingItemCount { get; set; }
    public int FailedListCount { get; set; }
    public int FailedItemCount { get; set; }
}
