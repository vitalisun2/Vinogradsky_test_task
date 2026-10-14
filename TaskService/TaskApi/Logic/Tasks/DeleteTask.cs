using Microsoft.EntityFrameworkCore;
using TaskApi.Data;

namespace TaskApi.Logic.Tasks;

public class DeleteTask(TaskDbContext dbContext)
{
    public async Task<bool> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deletedCount = await dbContext.TaskItems
            .Where(task => task.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedCount > 0;
    }
}