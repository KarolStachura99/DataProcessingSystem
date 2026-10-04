using System.Text.Json.Serialization;
using DataProcessingSystem.Data;
using DataProcessingSystem.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/tasks", async (CreateTaskRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Content))
    {
        return Results.BadRequest("Treść pliku CSV nie może być pusta.");
    }

    var task = new ProcessingTask
    {
        Id = Guid.NewGuid(),
        FileName = request.FileName,
        FileContent = request.Content,
        Status = ProcessingTaskStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    db.ProcessingTasks.Add(task);
    await db.SaveChangesAsync();

    return Results.Accepted($"/tasks/{task.Id}", new { task.Id, task.Status });
});

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
        task.Result,
        task.Error,
        task.CreatedAt,
        task.CompletedAt
    });
});

app.Run();