using Microsoft.EntityFrameworkCore;
using TaskApi.Api;
using TaskApi.Data;
using TaskApi.Infrastructure.Messaging;
using TaskApi.Logic.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration
    .GetConnectionString("TaskDb")
    ?? throw new InvalidOperationException(
        "Connection string 'TaskDb' is not configured.");

builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseNpgsql(connectionString));

// add endpoints
builder.Services.AddScoped<CreateTask>();
builder.Services.AddScoped<GetTasks>();
builder.Services.AddScoped<DeleteTask>();
builder.Services.AddScoped<CompleteTask>();

// Add RabbitMQ options and validate them
builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.HostName),
        "RabbitMq:HostName is required.")
    .Validate(
        options => options.Port is > 0 and <= 65535,
        "RabbitMq:Port must be between 1 and 65535.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.UserName)
            && !string.IsNullOrWhiteSpace(options.Password),
        "RabbitMq credentials are required.")
    .ValidateOnStart();

builder.Services.AddSingleton<RabbitMqTaskEventPublisher>();

builder.Services.AddSingleton<ITaskEventPublisher>(services =>
    services.GetRequiredService<RabbitMqTaskEventPublisher>());

builder.Services.AddHostedService<RabbitMqTaskEventPublisher>(services =>
    services.GetRequiredService<RabbitMqTaskEventPublisher>());

var app = builder.Build();

app.MapTaskEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.Run();

public partial class Program { }