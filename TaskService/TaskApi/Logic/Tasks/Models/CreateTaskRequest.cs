using TaskApi.Data.Entities;

namespace TaskApi.Logic.Tasks.Models;

public class CreateTaskRequest
{
    public string? Title { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
}