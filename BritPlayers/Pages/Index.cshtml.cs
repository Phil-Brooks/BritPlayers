using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BritPlayers.Models;
using BritPlayers.Services;

namespace BritPlayers.Pages;

public class IndexModel : PageModel
{
    public IReadOnlyList<PlayerEntry> Grandmasters { get; private set; } = new List<PlayerEntry>();
    public IReadOnlyList<PlayerEntry> OtherNotablePlayers { get; private set; } = new List<PlayerEntry>();

    public void OnGet()
    {
        Grandmasters = PlayerCatalog.All
            .Where(p => p.Title.Equals("Grandmaster", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => ExtractYear(p.Dates))
            .ToList();

        OtherNotablePlayers = PlayerCatalog.All
            .Where(p => !p.Title.Equals("Grandmaster", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => ExtractYear(p.Dates))
            .ToList();
    }

    private static int ExtractYear(string dates)
    {
        var match = Regex.Match(dates, @"\b\d{4}\b");
        return match.Success && int.TryParse(match.Value, out var year) ? year : int.MaxValue;
    }
}