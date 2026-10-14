using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Logic.Tasks.Models;

namespace TaskApi.Logic.Tasks;

public class CompleteTask(
    TaskDbContext dbContext, 
    ITaskEventPublisher publisher)
{
    public async Task<CompleteTaskResult> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.TaskItems
            .SingleOrDefaultAsync(
                task => task.Id == id,
                cancellationToken);

        if (task is null)
            return CompleteTaskResult.NotFound;

        if (task.IsCompleted)
            return CompleteTaskResult.Conflict;

        task.IsCompleted = true;
        var now = DateTimeOffset.UtcNow;

        task.CompletedAt = new DateTimeOffset(
            now.Ticks - now.Ticks % TimeSpan.TicksPerMicrosecond,
            TimeSpan.Zero);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return CompleteTaskResult.Conflict;
        }

        // Publish the task completed event
        var message = new TaskCompletedEvent(
            task.Id,
            task.Title,
            task.CompletedAt!.Value,
            task.Priority.ToString());

        await publisher.PublishCompletedAsync(
            message,
            CancellationToken.None);

        return CompleteTaskResult.Completed;
    }
}