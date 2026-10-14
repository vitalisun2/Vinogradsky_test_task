using TaskApi.Logic.Tasks.Models;

namespace TaskApi.Logic.Tasks;

public interface ITaskEventPublisher
{
    Task PublishCompletedAsync(
        TaskCompletedEvent message,
        CancellationToken cancellationToken);
}