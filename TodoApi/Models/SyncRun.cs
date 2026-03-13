using TodoApi.Enums;

namespace TodoApi.Models;

public class SyncRun
{
    public long Id { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public SyncRunStatus Status { get; set; }
    public SyncTriggerType TriggeredBy { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public string? ErrorSummary { get; set; }
}
