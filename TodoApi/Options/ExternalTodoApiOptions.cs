namespace TodoApi.Options;

public class ExternalTodoApiOptions
{
    public const string SectionName = "ExternalTodoApi";

    public string BaseUrl { get; set; } = "http://localhost";
    public int TimeoutSeconds { get; set; } = 10;
    public int RetryCount { get; set; } = 2;
    public int RetryDelayMs { get; set; } = 250;
}
