using System.Text.Json;
using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Api.Infrastructure;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISpoolRepository>();

        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var path = config["SeedDataPath"] ?? "../Data/spools.seed.json";
        var fullPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, path));

        if (!File.Exists(fullPath)) return;

        var json = await File.ReadAllTextAsync(fullPath);
        var spools = JsonSerializer.Deserialize<List<SeedSpool>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        await repo.SeedAsync(spools.Select(s => new Spool
        {
            Id = s.Id,
            PackageId = s.PackageId,
            SpoolNumber = s.SpoolNumber,
            DueDate = s.DueDate,
            Bom = s.Bom.Select(b => new BomItem(b.PartNumber, b.Quantity)).ToList(),
            StatusHistory = s.StatusHistory.Select(h => new StatusEvent(h.Station, h.ChangedAt, h.ChangedBy)).ToList()
        }));
    }

    private sealed record SeedSpool(string Id, string PackageId, string SpoolNumber, DateOnly DueDate, List<SeedBomItem> Bom, List<SeedStatusEvent> StatusHistory);
    private sealed record SeedBomItem(string PartNumber, int Quantity);
    private sealed record SeedStatusEvent(Station Station, DateTimeOffset ChangedAt, string ChangedBy);
}
