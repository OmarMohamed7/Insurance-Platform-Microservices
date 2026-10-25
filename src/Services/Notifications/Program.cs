using Microsoft.EntityFrameworkCore;
using Notifications.Application;
using Notifications.Application.Handlers;
using Notifications.Infrastructure;
using Notifications.Infrastructure.Consumer;
using Notifications.Infrastructure.Persistence;

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") is null or "Development")
    DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<NotificationDbContext>((_, options) =>
{
    var instance = builder.Configuration.GetConnectionString("NotificationDb")
        ?? throw new InvalidOperationException("ConnectionStrings:NotificationDb is not configured.");

    options.UseNpgsql(instance);
});

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ClaimEventNotificationHandler>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.Section));
builder.Services.AddHostedService<RabbitMqEventConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Containers start with an empty database, so let the service create its own tables when asked to.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.MigrateAsync();
}

app.Run();