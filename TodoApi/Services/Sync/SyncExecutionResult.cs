namespace TodoApi.Services.Sync;

public class SyncExecutionResult
{
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; } = [];
}
