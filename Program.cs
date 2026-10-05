using System.Text.Json.Serialization;
using DataProcessingSystem.Data;
using DataProcessingSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using DataProcessingSystem.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHostedService<TaskProcessingWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/tasks", async (IFormFile file, AppDbContext db) =>
{
    const long maxFileSize = 5 * 1024 * 1024;

    if (file.Length == 0)
    {
        return Results.BadRequest("Plik jest pusty.");
    }

    if (file.Length > maxFileSize)
    {
        return Results.BadRequest("Plik jest za duży. Maksymalny rozmiar to 5 MB.");
    }

    if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest("Dozwolone są tylko pliki .csv.");
    }

    using var reader = new StreamReader(file.OpenReadStream());
    var content = await reader.ReadToEndAsync();

    var task = new ProcessingTask
    {
        Id = Guid.NewGuid(),
        FileName = file.FileName,
        FileContent = content,
        Status = ProcessingTaskStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    db.ProcessingTasks.Add(task);
    await db.SaveChangesAsync();

    return Results.Accepted($"/tasks/{task.Id}", new { task.Id, task.Status });
})
.DisableAntiforgery();

app.MapGet("/tasks/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var task = await db.ProcessingTasks.FindAsync(id);

    if (task is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        task.Id,
        task.FileName,
        task.Status,
        Result = task.Result is null ? null : JsonSerializer.Deserialize<CsvAnalysisResult>(task.Result),
        task.Error,
        task.CreatedAt,
        task.CompletedAt
    });
});

app.Run();