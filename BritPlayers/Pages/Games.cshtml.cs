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

    // Expose catalog to the view without duplicating it
    public IReadOnlyList<PlayerEntry> Players => PlayerCatalog.All;

    public PlayerEntry SelectedPlayer { get; private set; } = null!;
    public ChessGame? SelectedGame { get; private set; }
    public IReadOnlyList<ChessGame> Games { get; private set; } = Array.Empty<ChessGame>();

    [BindProperty(SupportsGet = true)]
    public string? Player { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Game { get; set; }

    public IActionResult OnGet()
    {
        SelectedPlayer = PlayerCatalog.GetBySlug(Player);
        Games = _pgnService.GetGamesByFile(SelectedPlayer.PgnFileName);

        // Load requested game or preload the first game in the file
        SelectedGame = !string.IsNullOrWhiteSpace(Game)
            ? _pgnService.GetGameById(Game)
            : Games.FirstOrDefault();

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
}