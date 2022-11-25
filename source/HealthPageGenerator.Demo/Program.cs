using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using AspNetCore.HealthChecks.Generator;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDefinitionFileHealthChecks();

var app = builder.Build();
app.UseHttpsRedirection();
app.MapGet("/", (HttpContext httpContext) =>
{
    return Results.Ok(httpContext);
});

app.UseHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

app.UseHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.Run();
