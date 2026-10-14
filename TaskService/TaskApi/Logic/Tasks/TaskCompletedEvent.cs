namespace TaskApi.Logic.Tasks.Models;

public record TaskCompletedEvent(
    Guid TaskId,
    string Title,
    DateTimeOffset CompletedAt,
    string Priority);