using System.Text;
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

    public IActionResult OnGetDownloadGame(string sourceFile, string gameId)
    {
        var player = PlayerCatalog.All.FirstOrDefault(entry =>
            entry.PgnFileName.Equals(sourceFile, StringComparison.OrdinalIgnoreCase));
        if (player == null) return NotFound();

        var game = _pgnService.GetGamesByFile(player.PgnFileName)
            .FirstOrDefault(entry => entry.Id.Equals(gameId, StringComparison.Ordinal));
        if (game == null) return NotFound();

        var pgn = new StringBuilder();
        AppendTag(pgn, "Event", game.Event);
        AppendTag(pgn, "Site", game.Site);
        AppendTag(pgn, "Date", game.Date);
        AppendTag(pgn, "Round", game.Round);
        AppendTag(pgn, "White", game.White);
        AppendTag(pgn, "Black", game.Black);
        AppendTag(pgn, "Result", game.Result);
        AppendTag(pgn, "ECO", game.Eco);
        pgn.AppendLine();
        pgn.AppendLine(game.Moves);

        return File(Encoding.UTF8.GetBytes(pgn.ToString()), "application/x-chess-pgn",
            $"{player.Slug}-{game.Id[..8]}.pgn");
    }

    private static void AppendTag(StringBuilder pgn, string name, string value)
    {
        var escapedValue = value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ');
        pgn.Append('[').Append(name).Append(" \"").Append(escapedValue).AppendLine("\"]");
    }
}