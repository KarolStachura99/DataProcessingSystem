using DataProcessingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DataProcessingSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ProcessingTask> ProcessingTasks => Set<ProcessingTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessingTask>()
            .Property(t => t.Status)
            .HasConversion<string>();
    }
}