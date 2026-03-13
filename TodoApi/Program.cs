using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using TodoApi.Options;
using TodoApi.Services.ExternalApi;
using TodoApi.Services.Sync;

var builder = WebApplication.CreateBuilder(args);

var todoConnectionString = builder.Configuration.GetConnectionString("TodoContext")
    ?? throw new InvalidOperationException("Connection string 'TodoContext' was not found.");

builder.Services.Configure<ExternalTodoApiOptions>(builder.Configuration.GetSection(ExternalTodoApiOptions.SectionName));
builder.Services.Configure<SyncOptions>(builder.Configuration.GetSection(SyncOptions.SectionName));

builder.Services.AddDbContext<TodoContext>(opt => opt.UseSqlServer(todoConnectionString));
builder.Services.AddHttpClient<IExternalTodoApiClient, ExternalTodoApiClient>();
builder.Services.AddScoped<ISyncPlanner, SyncPlanner>();
builder.Services.AddScoped<ISyncExecutor, SyncExecutor>();
builder.Services.AddScoped<ITodoSyncService, TodoSyncService>();
builder.Services.AddScoped<TodoSyncRecurringJob>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(todoConnectionString, new SqlServerStorageOptions
    {
        PrepareSchemaIfNecessary = true,
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(15),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,
    }));

builder.Services.AddHangfireServer();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.UseCors("frontend");
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TodoContext>();

    await context.Database.ExecuteSqlRawAsync(@"
UPDATE TodoList
SET SyncStatus = 'Synced'
WHERE SyncStatus IS NULL OR LTRIM(RTRIM(SyncStatus)) = '';

UPDATE TodoItem
SET SyncStatus = 'Synced'
WHERE SyncStatus IS NULL OR LTRIM(RTRIM(SyncStatus)) = '';

UPDATE TodoList
SET LastModifiedAtUtc = SYSUTCDATETIME()
WHERE LastModifiedAtUtc = '0001-01-01T00:00:00.0000000';

UPDATE TodoItem
SET LastModifiedAtUtc = SYSUTCDATETIME()
WHERE LastModifiedAtUtc = '0001-01-01T00:00:00.0000000';
");

    var syncOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SyncOptions>>().Value;
    if (syncOptions.Enabled)
    {
        RecurringJob.AddOrUpdate<TodoSyncRecurringJob>(
            "todo-sync",
            job => job.RunAsync(),
            syncOptions.RecurringCron);
    }
}

app.Run();
