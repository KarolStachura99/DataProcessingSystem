namespace DataProcessingSystem.Models;

public class ProcessingTask
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileContent { get; set; } = string.Empty;
    public ProcessingTaskStatus Status { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}