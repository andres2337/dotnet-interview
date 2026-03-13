using Microsoft.EntityFrameworkCore;
using TodoApi.Enums;
using TodoApi.Models;

public class TodoContext : DbContext
{
    public TodoContext(DbContextOptions<TodoContext> options)
        : base(options) { }

    public DbSet<TodoList> TodoList { get; set; } = default!;
    public DbSet<TodoItem> TodoItem { get; set; } = default!;
    public DbSet<SyncRun> SyncRun { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TodoList>()
            .Property(x => x.SyncStatus)
            .HasConversion<string>();

        modelBuilder.Entity<TodoItem>()
            .Property(x => x.SyncStatus)
            .HasConversion<string>();

        modelBuilder.Entity<SyncRun>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder.Entity<SyncRun>()
            .Property(x => x.TriggeredBy)
            .HasConversion<string>();

        modelBuilder.Entity<TodoList>()
            .HasIndex(x => x.ExternalId);

        modelBuilder.Entity<TodoList>()
            .HasIndex(x => x.SyncStatus);

        modelBuilder.Entity<TodoList>()
            .HasIndex(x => x.LastModifiedAtUtc);

        modelBuilder.Entity<TodoItem>()
            .HasIndex(x => x.ExternalId);

        modelBuilder.Entity<TodoItem>()
            .HasIndex(x => x.SyncStatus);

        modelBuilder.Entity<TodoItem>()
            .HasIndex(x => x.LastModifiedAtUtc);

        modelBuilder.Entity<SyncRun>()
            .HasIndex(x => x.StartedAtUtc);
    }
}
