using Claims.Api.ExceptionHandling;
using Claims.Application;
using Claims.Application.Handlers;
using Claims.Infrastructure;
using Claims.Infrastructure.Messaging;
using Claims.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") is null or "Development")
    DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ClaimsDbContext>((provider, options) =>
{
    var cs = builder.Configuration.GetConnectionString("ClaimsDb")
             ?? throw new InvalidOperationException("ConnectionStrings:ClaimsDb is not configured.");
    options.UseNpgsql(cs);

});

builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<SubmitClaimHandler>();
builder.Services.AddScoped<GetClaimHandler>();
builder.Services.AddScoped<ListClaimsHandler>();
builder.Services.AddScoped<StartReviewClaimHandler>();
builder.Services.AddScoped<ApproveClaimHandler>();
builder.Services.AddScoped<RejectClaimHandler>();


builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.Section));
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();