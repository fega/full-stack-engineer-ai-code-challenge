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
/// Drives the real <c>POST /api/spools/{id}/advance</c> route end to end so the HTTP
/// contract (status codes + body) is verified, not just the service.
///
/// Each test builds its own <see cref="Factory"/> (and therefore its own singleton
/// repository) so there is no shared mutable state between tests — order and
/// parallelism cannot leak spools from one test into another.
/// </summary>
public class AdvanceEndpointTests
{
    private sealed class Factory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Disable file-based auto-seeding: SeedData.InitializeAsync only
                    // seeds when the configured path exists, so a path that cannot
                    // exist guarantees an empty, deterministic repository. Tests then
                    // seed exactly the spools they need below.
                    ["SeedDataPath"] = "__no_seed_file__.json"
                }));
            return base.CreateHost(builder);
        }
    }

    private static async Task SeedAsync(WebApplicationFactory<Program> factory, params Spool[] spools)
    {
        var repo = factory.Services.GetRequiredService<ISpoolRepository>();
        await repo.SeedAsync(spools);
    }

    private sealed record AdvanceResponse(string Id, string CurrentStation);
    private sealed record ErrorBody(string Message);

    // Mirrors the JSON the API emits for GET /api/spools/{id}. With the global
    // JsonStringEnumConverter, CurrentStation is the station name (e.g. "Cut").
    private sealed record SpoolView(string Id, string CurrentStation);

    [Fact]
    public async Task Advance_known_spool_returns_200_with_new_station()
    {
        using var factory = new Factory();
        await SeedAsync(factory, SpoolBuilder.At("HTTP-1", Station.Detailing, DateTimeOffset.UtcNow.AddDays(-1)));
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/spools/HTTP-1/advance", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdvanceResponse>();
        Assert.Equal("HTTP-1", body!.Id);
        Assert.Equal("Cut", body.CurrentStation);

        // The change is observable on a subsequent read, with the same string form.
        var spool = await client.GetFromJsonAsync<SpoolView>("/api/spools/HTTP-1");
        Assert.Equal("Cut", spool!.CurrentStation);
    }

    [Fact]
    public async Task Advance_unknown_spool_returns_404_with_message()
    {
        using var factory = new Factory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/spools/UNKNOWN-ID/advance", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal("Spool not found", body!.Message);
    }

    [Fact]
    public async Task Advance_past_Installed_returns_400_with_message()
    {
        using var factory = new Factory();
        await SeedAsync(factory, SpoolBuilder.At("HTTP-2", Station.Installed, DateTimeOffset.UtcNow.AddDays(-1)));
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/spools/HTTP-2/advance", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal("Spool cannot move backward or beyond Installed", body!.Message);
    }
}
