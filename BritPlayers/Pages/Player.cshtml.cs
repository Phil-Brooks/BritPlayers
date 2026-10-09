using BritPlayers.Models;
using BritPlayers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BritPlayers.Pages;

public class PlayerModel : PageModel
{
    private readonly PgnService _pgnService;

    public PlayerModel(PgnService pgnService)
    {
        _pgnService = pgnService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Player { get; set; }

    public PlayerEntry CurrentPlayer { get; private set; } = null!;
    public int GameCount { get; private set; }

    public void OnGet()
    {
        CurrentPlayer = PlayerCatalog.GetBySlug(Player);
        var games = _pgnService.GetGamesByFile(CurrentPlayer.PgnFileName);
        GameCount = games.Count;
    }
}