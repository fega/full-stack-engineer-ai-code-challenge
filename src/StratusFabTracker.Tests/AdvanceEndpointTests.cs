using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;
using Xunit;

namespace StratusFabTracker.Tests;

/// <summary>
/// Drives the real <c>POST /api/spools/{id}/advance</c> route end to end so the
/// HTTP contract (status codes + body) is verified, not just the service.
/// Auto-seeding is disabled via a bogus seed path; tests seed known spools instead.
/// </summary>
public class AdvanceEndpointTests : IClassFixture<AdvanceEndpointTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Point at a file that does not exist so SeedData.InitializeAsync
                    // short-circuits and the repository starts empty/deterministic.
                    ["SeedDataPath"] = "__no_seed__.json"
                }));
            return base.CreateHost(builder);
        }
    }

    private readonly Factory _factory;

    public AdvanceEndpointTests(Factory factory) => _factory = factory;

    private async Task SeedAsync(params Spool[] spools)
    {
        var repo = _factory.Services.GetRequiredService<ISpoolRepository>();
        await repo.SeedAsync(spools);
    }

    private sealed record AdvanceResponse(string Id, string CurrentStation);

    [Fact]
    public async Task Advance_known_spool_returns_200_with_new_station()
    {
        await SeedAsync(SpoolBuilder.At("HTTP-1", Station.Detailing, DateTimeOffset.UtcNow.AddDays(-1)));
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/spools/HTTP-1/advance", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdvanceResponse>();
        Assert.Equal("HTTP-1", body!.Id);
        Assert.Equal("Cut", body.CurrentStation);

        // The change is observable on a subsequent read (enum serializes numerically).
        var spool = await client.GetFromJsonAsync<SpoolView>("/api/spools/HTTP-1");
        Assert.Equal((int)Station.Cut, spool!.CurrentStation);
    }

    [Fact]
    public async Task Advance_unknown_spool_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/spools/UNKNOWN-ID/advance", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Advance_past_Installed_returns_400()
    {
        await SeedAsync(SpoolBuilder.At("HTTP-2", Station.Installed, DateTimeOffset.UtcNow.AddDays(-1)));
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/spools/HTTP-2/advance", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Mirrors the JSON the API emits for GET /api/spools/{id}; CurrentStation is a
    // computed Station enum, serialized numerically by System.Text.Json defaults.
    private sealed record SpoolView(string Id, int CurrentStation);
}
