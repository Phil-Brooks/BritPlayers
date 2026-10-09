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
    public string BioHtml { get; private set; } = string.Empty;

    public void OnGet()
    {
        CurrentPlayer = PlayerCatalog.GetBySlug(Player);
        var games = _pgnService.GetGamesByFile(CurrentPlayer.PgnFileName);
        GameCount = games.Count;

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