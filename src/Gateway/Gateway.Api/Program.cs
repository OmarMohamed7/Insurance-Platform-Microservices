var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// The only Swagger UI in the system. Each service publishes just its OpenAPI document (Development only),
// which the gateway proxies under /docs/<service>/openapi.json. Off unless Swagger:Enabled is true.
if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/docs/claims/openapi.json", "Claims");
        options.SwaggerEndpoint("/docs/notifications/openapi.json", "Notifications");
    });
}

app.MapGet("/health", () => Results.Ok());

app.MapReverseProxy();

app.Run();
