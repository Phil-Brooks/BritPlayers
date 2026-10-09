using BritPlayers.Models;
using BritPlayers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BritPlayers.Pages;

public class GamesModel : PageModel
{
    private readonly PgnService _pgnService;

    public GamesModel(PgnService pgnService)
    {
        _pgnService = pgnService;
    }

    public IReadOnlyList<PlayerEntry> Players { get; } =
    [
        new("Murray Chandler", "murray-chandler", "chandlerm.pgn"),
    new("Paul Littlewood", "paul-littlewood", "littlewoodpaul.pgn"),
    new("Anthony J Miles", "anthony-j-miles", "milestony.pgn"),
    new("Nigel Short", "nigel-short", "shortn.pgn"),
    new("Ian D Wells", "ian-d-wells", "wells_ian.pgn")
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

        // Load all games directly from this player's dedicated PGN file
        Games = _pgnService.GetGamesByFile(SelectedPlayer.PgnFileName);

        // Preload the requested game or default to the first one in the file
        if (!string.IsNullOrWhiteSpace(Game))
        {
            SelectedGame = _pgnService.GetGameById(Game) ?? Games.FirstOrDefault();
        }
        else if (Games.Count > 0)
        {
            SelectedGame = Games[0];
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

    public sealed record PlayerEntry(string Name, string Slug, string PgnFileName);
}