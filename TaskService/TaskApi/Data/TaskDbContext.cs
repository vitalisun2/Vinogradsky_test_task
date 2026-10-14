using Microsoft.EntityFrameworkCore;
using TaskApi.Data.Entities;

namespace TaskApi.Data;

public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var task = modelBuilder.Entity<TaskItem>();

        task.HasKey(t => t.Id);

        task.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        task.Property(t => t.RowVersion)
            .IsRowVersion();
    }
}
