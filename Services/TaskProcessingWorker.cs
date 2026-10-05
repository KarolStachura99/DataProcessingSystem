using System.Text.Json;
using DataProcessingSystem.Data;
using DataProcessingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DataProcessingSystem.Services;

public class TaskProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TaskProcessingWorker> _logger;

    public TaskProcessingWorker(IServiceScopeFactory scopeFactory, ILogger<TaskProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessNextTaskAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessNextTaskAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var task = await db.ProcessingTasks
            .Where(t => t.Status == ProcessingTaskStatus.Pending)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync(stoppingToken);

        if (task is null)
        {
            return;
        }

        task.Status = ProcessingTaskStatus.Processing;
        await db.SaveChangesAsync(stoppingToken);

        try
        {
            var result = CsvAnalyzer.Analyze(task.FileContent);
            task.Result = JsonSerializer.Serialize(result);
            task.Status = ProcessingTaskStatus.Completed;
        }
        catch (FormatException ex)
        {
            task.Error = ex.Message;
            task.Status = ProcessingTaskStatus.Failed;
        }

        task.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(stoppingToken);

        _logger.LogInformation("Zadanie {TaskId} zakończone ze statusem {Status}.", task.Id, task.Status);
    }
}