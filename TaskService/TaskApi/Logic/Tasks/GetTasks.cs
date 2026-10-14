using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Logic.Tasks.Models;

namespace TaskApi.Logic.Tasks;

public class GetTasks(TaskDbContext dbContext)
{
    public async Task<List<TaskResponse>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.TaskItems
            .Select(task => new TaskResponse(
                task.Id,
                task.Title,
                task.IsCompleted,
                task.CreatedAt,
                task.CompletedAt,
                task.Priority))
            .ToListAsync(cancellationToken);
    }
}