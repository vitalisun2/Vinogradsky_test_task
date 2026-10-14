using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskApi.Data;
using TaskApi.Logic.Tasks.Models;

namespace TaskApi.Tests;

public class CompleteTaskIntegrationTest(TaskApiFixture fixture)
    : IClassFixture<TaskApiFixture>
{
    [Fact]
    public async Task CompleteTask_SavesCompletionAndPublishesEvent()
    {
        // Arrange
        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/tasks",
            new { title = "Integration test" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created =
            await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        Assert.NotNull(created);
        Assert.False(created.IsCompleted);
        Assert.Null(created.CompletedAt);

        // Act
        using var completeResponse = await fixture.Client.PutAsync(
            $"/tasks/{created.Id}/complete",
            content: null);

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            completeResponse.StatusCode);

        await using var scope = fixture.Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<TaskDbContext>();

        var saved = await db.TaskItems
            .AsNoTracking()
            .SingleAsync(task => task.Id == created.Id);

        Assert.True(saved.IsCompleted);
        Assert.NotNull(saved.CompletedAt);

        var message = Assert.Single(
            fixture.Publisher.Messages,
            item => item.TaskId == created.Id);

        Assert.Equal(saved.Id, message.TaskId);
        Assert.Equal(saved.Title, message.Title);
        Assert.Equal(saved.CompletedAt.Value, message.CompletedAt);
        Assert.Equal("Medium", message.Priority);
    }

    [Fact]
    public async Task CompleteTask_ConcurrentRequests_ReturnSuccessAndConflict()
    {
        using var createResponse = await fixture.Client.PostAsJsonAsync(
            "/tasks",
            new { title = "Concurrent completion" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created =
            await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        Assert.NotNull(created);

        var url = $"/tasks/{created.Id}/complete";

        var responses = await Task.WhenAll(
            fixture.Client.PutAsync(url, content: null),
            fixture.Client.PutAsync(url, content: null));

        try
        {
            Assert.Single(responses,
                response => response.StatusCode == HttpStatusCode.NoContent);

            Assert.Single(responses,
                response => response.StatusCode == HttpStatusCode.Conflict);

            Assert.Single(fixture.Publisher.Messages,
                message => message.TaskId == created.Id);
        }
        finally
        {
            foreach (var response in responses)
                response.Dispose();
        }
    }
}