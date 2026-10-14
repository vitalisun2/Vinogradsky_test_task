using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Data.Entities;
using TaskApi.Logic.Tasks.Models;

namespace TaskApi.Logic.Tasks;

public class CreateTask(TaskDbContext dbContext)
{
    public async Task<TaskResponse?> ExecuteAsync(
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title)
            || request.Title.Length > 200
            || !Enum.IsDefined(request.Priority))
        {
            return null;
        }

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Priority = request.Priority,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.TaskItems.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TaskResponse(
            task.Id,
            task.Title,
            task.IsCompleted,
            task.CreatedAt,
            task.CompletedAt,
            task.Priority);
    }
}