using TaskApi.Data.Entities;

namespace TaskApi.Logic.Tasks.Models;

public record TaskResponse(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    Priority Priority);