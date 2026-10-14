using TaskApi.Logic.Tasks;
using TaskApi.Logic.Tasks.Models;

namespace TaskApi.Api;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this WebApplication app)
    {
        app.MapPost("/tasks", async (
            CreateTaskRequest request,
            CreateTask createTask,
            CancellationToken cancellationToken) =>
        {
            var response = await createTask.ExecuteAsync(
                request, cancellationToken);

            if (response is null)
            {
                return Results.BadRequest(new
                {
                    error = "Title обязателен и не длиннее 200 символов; "
                        + "Priority должен быть допустимым значением."
                });
            }

            return Results.Created($"/tasks/{response.Id}", response);
        });

        app.MapGet("/tasks", async (
            GetTasks getTasks,
            CancellationToken cancellationToken) =>
            {
                var response = await getTasks.ExecuteAsync(cancellationToken);
                return Results.Ok(response);
            });

        app.MapDelete("/tasks/{id}", async (
            Guid id,
            DeleteTask deleteTask,
            CancellationToken cancellationToken) =>
            {
                var deleted = await deleteTask.ExecuteAsync(id, cancellationToken);

                return deleted
                    ? Results.NoContent()
                    : Results.NotFound();
            });

        app.MapPut("/tasks/{id}/complete", async (
            Guid id,
            CompleteTask completeTask,
            CancellationToken cancellationToken) =>
            {
                var result = await completeTask.ExecuteAsync(id, cancellationToken);

                return result switch
                {
                    CompleteTaskResult.Completed => Results.NoContent(),
                    CompleteTaskResult.NotFound => Results.NotFound(),
                    CompleteTaskResult.Conflict => Results.Conflict(),
                    _ => Results.StatusCode(500)
                };
            });
    }
}