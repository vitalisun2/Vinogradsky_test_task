using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text.Json;
using TaskApi.Logic.Tasks;
using TaskApi.Logic.Tasks.Models;

namespace TaskApi.Infrastructure.Messaging;

public class RabbitMqTaskEventPublisher : ITaskEventPublisher, IHostedService, IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqTaskEventPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);
    private volatile bool _stopping;
    private int _disposed;

    public RabbitMqTaskEventPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqTaskEventPublisher> logger)
    {
        var settings = options.Value;

        _factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            AutomaticRecoveryEnabled = false
        };

        _logger = logger;
    }

    public async Task PublishCompletedAsync(
        TaskCompletedEvent message,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);

        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(3));

        var lockTaken = false;

        try
        {
            await _publishLock.WaitAsync(timeout.Token);
            lockTaken = true;

            // Check if the publisher is stopping before attempting to publish the message
            if (_stopping)
                throw new InvalidOperationException("Publisher is stopping.");

            await EnsureConnectedAsync(timeout.Token);

            await _channel!.BasicPublishAsync(
                exchange: "task.events",
                routingKey: "task.completed",
                mandatory: false,
                basicProperties: new BasicProperties
                {
                    ContentType = "application/json",
                    Persistent = true
                },
                body: System.Text.Encoding.UTF8.GetBytes(json),
                cancellationToken: timeout.Token);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish task.completed. Message: {Message}",
                json);
        }
        finally
        {
            if (lockTaken)
                _publishLock.Release();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ shutdown started.");

        _stopping = true;

        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        var lockTaken = false;

        try
        {
            await _publishLock.WaitAsync(timeout.Token);
            lockTaken = true;

            if (_channel is { IsOpen: true })
                await _channel.CloseAsync(timeout.Token);

            if (_connection is { IsOpen: true })
                await _connection.CloseAsync(timeout.Token);

            _logger.LogInformation("RabbitMQ shutdown completed.");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "RabbitMQ graceful shutdown did not complete.");
        }
        finally
        {
            if (lockTaken)
                _publishLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            if (_channel is not null)
                await _channel.DisposeAsync();
        }
        finally
        {
            if (_connection is not null)
                await _connection.DisposeAsync();
        }
    }

    private async Task EnsureConnectedAsync(
        CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);
        await EnsureChannelAsync(cancellationToken);

        await _channel!.ExchangeDeclareAsync(
            exchange: "task.events",
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
    }

    private async Task EnsureConnectionAsync(
        CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            return;

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
            await _connection.DisposeAsync();

        _connection = await _factory.CreateConnectionAsync(
            cancellationToken);
    }

    private async Task EnsureChannelAsync(
        CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return;

        if (_channel is not null)
            await _channel.DisposeAsync();

        _channel = await _connection!.CreateChannelAsync(
            cancellationToken: cancellationToken);
    }


}