using System.Text.Json;
using BritPlayers.Models;
using BritPlayers.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BritPlayers.Pages;

public class IndexModel : PageModel
{
    private readonly IWebHostEnvironment _environment;

    public IndexModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public IReadOnlyList<PlayerEntry> Players { get; private set; } = Array.Empty<PlayerEntry>();
    public FideRatingData FideRatings { get; private set; } = new();

    public void OnGet()
    {
        Players = PlayerCatalog.All
            .OrderBy(player => player.Name.Split(' ').Last(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ratingsPath = Path.Combine(_environment.ContentRootPath, "Data", "fide-ratings.json");
        FideRatings = JsonSerializer.Deserialize<FideRatingData>(
            System.IO.File.ReadAllText(ratingsPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException($"FIDE ratings data at '{ratingsPath}' is empty or invalid.");
    }
}