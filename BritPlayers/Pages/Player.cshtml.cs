using System.Text.RegularExpressions;
using BritPlayers.Models;
using BritPlayers.Services;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BritPlayers.Pages;

public class PlayerModel : PageModel
{
    private readonly PgnService _pgnService;
    private readonly IWebHostEnvironment _env;

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions() // Supports tables, emphasis, task lists, etc.
        .Build();

    public PlayerModel(PgnService pgnService, IWebHostEnvironment env)
    {
        _pgnService = pgnService;
        _env = env;
    }

    [BindProperty(SupportsGet = true)]
    public string? Player { get; set; }

    public PlayerEntry CurrentPlayer { get; private set; } = null!;
    public int GameCount { get; private set; }
    public int Wins { get; private set; }
    public int Draws { get; private set; }
    public int Losses { get; private set; }
    public int? EarliestGameYear { get; private set; }
    public int? LatestGameYear { get; private set; }
    public string BioHtml { get; private set; } = string.Empty;
    public string? PhotoUrl { get; private set; }

    private static bool IsPlayer(string gamePlayer, PlayerEntry player)
    {
        var profileTokens = Regex.Split(player.Name, @"[^\p{L}\p{N}]+")
            .Where(token => token.Length > 0)
            .ToArray();
        if (profileTokens.Length < 2) return false;

        string surname;
        string[] givenNames;
        var commaIndex = gamePlayer.IndexOf(',');
        if (commaIndex >= 0)
        {
            surname = gamePlayer[..commaIndex].Trim();
            givenNames = Regex.Split(gamePlayer[(commaIndex + 1)..].Trim(), @"[^\p{L}\p{N}]+")
                .Where(token => token.Length > 0)
                .ToArray();
        }
        else
        {
            var gameTokens = Regex.Split(gamePlayer, @"[^\p{L}\p{N}]+")
                .Where(token => token.Length > 0)
                .ToArray();
            if (gameTokens.Length < 2) return false;
            surname = gameTokens[^1];
            givenNames = gameTokens[..^1];
        }

        var profileGivenNames = profileTokens[..^1];
        var sharesGivenName = givenNames.Any(givenName => profileGivenNames.Any(profileGivenName =>
            givenName.Equals(profileGivenName, StringComparison.OrdinalIgnoreCase)
            || (givenName.Length == 1 && profileGivenName.StartsWith(givenName, StringComparison.OrdinalIgnoreCase))
            || (profileGivenName.Length == 1 && givenName.StartsWith(profileGivenName, StringComparison.OrdinalIgnoreCase))));

        return surname.Equals(profileTokens[^1], StringComparison.OrdinalIgnoreCase) && sharesGivenName;
    }

    public void OnGet()
    {
        CurrentPlayer = PlayerCatalog.GetBySlug(Player);
        var games = _pgnService.GetGamesByFile(CurrentPlayer.PgnFileName);
        GameCount = games.Count;

        var playerGames = games.Where(game => IsPlayer(game.White, CurrentPlayer)
                || IsPlayer(game.Black, CurrentPlayer))
            .ToList();

        foreach (var game in playerGames)
        {
            var playerIsWhite = IsPlayer(game.White, CurrentPlayer);
            switch (game.Result)
            {
                case "1-0":
                    if (playerIsWhite) Wins++;
                    else Losses++;
                    break;
                case "0-1":
                    if (playerIsWhite) Losses++;
                    else Wins++;
                    break;
                case "1/2-1/2":
                    Draws++;
                    break;
            }
        }

        var gameYears = playerGames
            .Select(game => Regex.Match(game.Date, @"^\d{4}").Value)
            .Where(year => int.TryParse(year, out _))
            .Select(int.Parse)
            .ToList();
        if (gameYears.Count > 0)
        {
            EarliestGameYear = gameYears.Min();
            LatestGameYear = gameYears.Max();
        }

        var photoPath = Path.Combine(_env.WebRootPath, "photos", $"{CurrentPlayer.Slug}.jpg");
        if (System.IO.File.Exists(photoPath))
        {
            PhotoUrl = $"/photos/{CurrentPlayer.Slug}.jpg";
        }

        // Path to Markdown file: Data/bios/{slug}.md
        var bioPath = Path.Combine(_env.ContentRootPath, "Data", "bios", $"{CurrentPlayer.Slug}.md");

        if (System.IO.File.Exists(bioPath))
        {
            var markdown = System.IO.File.ReadAllText(bioPath);
            BioHtml = Markdown.ToHtml(markdown, Pipeline);
        }
        else
        {
            BioHtml = "<p class='text-muted'>Biographical notes coming soon.</p>";
        }
    }
}