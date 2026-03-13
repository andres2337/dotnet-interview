namespace TodoApi.Options;

public class SyncOptions
{
    public const string SectionName = "Sync";

    public bool Enabled { get; set; } = true;
    public string RecurringCron { get; set; } = "*/2 * * * *";
}
