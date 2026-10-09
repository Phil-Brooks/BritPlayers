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
        new("Murray Chandler", "murray-chandler", ["Chandler, Murray"]),
        new("Paul Littlewood", "paul-littlewood", ["Littlewood, Paul E", "Littlewood, Paul"]),
        new("Anthony J Miles", "anthony-j-miles", ["Miles, Anthony J", "Miles, Anthony"]),
        new("Nigel Short", "nigel-short", ["Short, Nigel"]),
        new("Ian D Wells", "ian-d-wells", ["Wells, Ian D", "Wells, Ian"])
    ];

    public PlayerEntry SelectedPlayer { get; private set; } = null!;

    public ChessGame? SelectedGame { get; private set; }

    public IReadOnlyList<ChessGame> Games { get; private set; } = Array.Empty<ChessGame>();

    [BindProperty(SupportsGet = true)]
    public string? Player { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Game { get; set; }

    public IActionResult OnGet()
    {
        SelectedPlayer = Players.FirstOrDefault(p => p.Slug.Equals(Player, StringComparison.OrdinalIgnoreCase))
            ?? Players[0];

        // Load games for the selected player
        Games = SelectedPlayer.Aliases
            .SelectMany(alias => _pgnService.Search(alias, ""))
            .DistinctBy(g => g.Id)
            .Take(250)
            .ToList();

        // If a specific game was requested via ?game=..., load it; 
        // otherwise, preload the very first game in the list
        if (!string.IsNullOrWhiteSpace(Game))
        {
            SelectedGame = _pgnService.GetGameById(Game);
        }
        else if (Games.Count > 0)
        {
            // Preload first game
            SelectedGame = _pgnService.GetGameById(Games[0].Id) ?? Games[0];
        }

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return Partial("_GameListPartial", Games);
        }

        return Page();
    }
    public IActionResult OnGetGameViewer(string gameId)
    {
        var game = _pgnService.GetGameById(gameId);
        if (game == null) return NotFound();
        return Partial("_GameReplayerPartial", game);
    }

    public sealed record PlayerEntry(string Name, string Slug, IReadOnlyList<string> Aliases);
}