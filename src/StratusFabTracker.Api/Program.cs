using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;
using StratusFabTracker.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ISpoolRepository, InMemorySpoolRepository>();
builder.Services.AddSingleton<SpoolWorkflowService>();
builder.Services.AddSingleton<DashboardService>();
builder.Services.AddSingleton<SpoolQueryService>();
builder.Services.AddSingleton<ThroughputService>();

var app = builder.Build();
app.UseCors();

await SeedData.InitializeAsync(app.Services);

app.MapGet("/api/dashboard", async (DashboardService service) => Results.Ok(await service.GetDashboardAsync()));
app.MapGet("/api/throughput", async (ThroughputService service) => Results.Ok(await service.GetThroughputAsync()));
app.MapGet("/api/spools", async (SpoolQueryService service) => Results.Ok(await service.GetSummariesAsync()));
app.MapGet("/api/spools/{id}", async (string id, ISpoolRepository repo) =>
{
    var spool = await repo.GetByIdAsync(id);
    return spool is null ? Results.NotFound() : Results.Ok(spool);
});

app.MapPost("/api/spools/{id}/advance", async (string id, SpoolWorkflowService service) =>
{
    var result = await service.AdvanceAsync(id);
    return result switch
    {
        TransitionResult.NotFound => Results.NotFound(new { message = "Spool not found" }),
        TransitionResult.InvalidTransition => Results.BadRequest(new { message = "Spool cannot move backward or beyond Installed" }),
        TransitionResult.Success => Results.NoContent(),
        _ => Results.StatusCode(500)
    };
});

app.Run();

public partial class Program { }
