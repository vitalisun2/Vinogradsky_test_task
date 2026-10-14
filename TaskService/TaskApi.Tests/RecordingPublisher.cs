using System.Collections.Concurrent;
using TaskApi.Logic.Tasks;
using TaskApi.Logic.Tasks.Models;

namespace TaskApi.Tests;

public sealed class RecordingPublisher : ITaskEventPublisher
{
    public ConcurrentQueue<TaskCompletedEvent> Messages { get; } = new();

    public Task PublishCompletedAsync(
        TaskCompletedEvent message,
        CancellationToken cancellationToken)
    {
        Messages.Enqueue(message);
        return Task.CompletedTask;
    }
}