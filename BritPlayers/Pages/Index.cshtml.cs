using BritPlayers.Models;
using BritPlayers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BritPlayers.Pages;

public class IndexModel : PageModel
{
    private readonly PgnService _pgnService;

    public IndexModel(PgnService pgnService)
    {
        _pgnService = pgnService;
    }

    public IReadOnlyList<PlayerEntry> Players { get; } =
    [
        new("Murray Chandler", "murray-chandler", "Chandler, Murray"),
        new("Paul Littlewood", "paul-littlewood", "Littlewood, Paul E;Littlewood, Paul"),
        new("Anthony J Miles", "anthony-j-miles", "Miles, Anthony J;Miles, Anthony"),
        new("Nigel Short", "nigel-short", "Short, Nigel"),
        new("Ian D Wells", "ian-d-wells", "Wells, Ian D;Wells, Ian")
    ];

    public PlayerEntry? SelectedPlayer { get; private set; }

    public ChessGame? SelectedGame { get; private set; }

    public IReadOnlyList<ChessGame> Games { get; private set; } = Array.Empty<ChessGame>();

    [BindProperty(SupportsGet = true)]
    public string? Player { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Game { get; set; }

    public void OnGet()
    {
        var selected = Players.FirstOrDefault(p => p.Slug.Equals(Player, StringComparison.OrdinalIgnoreCase))
            ?? Players[0];
        SelectedPlayer = selected;

        var aliases = selected.Aliases.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Games = _pgnService.SearchByPlayer(aliases).Take(250).ToList();
        SelectedGame = _pgnService.GetGameById(Game);
    }

    public sealed record PlayerEntry(string Name, string Slug, string Aliases);
}
