using Microsoft.AspNetCore.Mvc.RazorPages;
using BritPlayers.Services;

namespace BritPlayers.Pages;

public class IndexModel : PageModel
{
    public class PlayerItem
    {
        public string Name { get; set; }
        public int? Year { get; set; }
        public string? Slug { get; set; }
        public bool IsAvailable => !string.IsNullOrEmpty(Slug);

        public PlayerItem(string name, int? year = null, string? slug = null)
        {
            Name = name;
            Year = year;
            Slug = slug;
        }
    }

    public List<PlayerItem> Grandmasters { get; private set; } = new();
    public List<PlayerItem> OtherNotablePlayers { get; private set; } = new();

    public void OnGet()
    {
        // Finds the slug from PlayerCatalog.All if the player is already archived
        string? FindSlug(string query) =>
            PlayerCatalog.All.FirstOrDefault(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Slug.Contains(query, StringComparison.OrdinalIgnoreCase))?.Slug;

        Grandmasters = new List<PlayerItem>
        {
            new("Miles, Anthony J", 1976, FindSlug("Miles")),
            new("Keene, Raymond D", 1976, FindSlug("Keene")),
            new("Stean, Michael F", 1977, FindSlug("Stean")),
            new("Nunn, John DM", 1978, FindSlug("Nunn")),
            new("Speelman, Jonathan S", 1980, FindSlug("Speelman")),
            new("Mestel, A Jonathan", 1982, FindSlug("Mestel")),
            new("Chandler, Murray G", 1983, FindSlug("Chandler")),
            new("Short, Nigel D", 1984, FindSlug("Short")),
            new("Plaskett, H James", 1985, FindSlug("Plaskett")),
            new("Flear, Glenn C", 1987, FindSlug("Flear")),
            new("Hodgson, Julian", 1988, FindSlug("Hodgson")),
            new("King, Daniel J", 1989, FindSlug("King")),
            new("Adams, Michael", 1989, FindSlug("Adams")),
            new("Norwood, David", 1989, FindSlug("Norwood"))
        };

        OtherNotablePlayers = new List<PlayerItem>
        {
            new("Littlewood, Paul", slug: FindSlug("Littlewood")),
            new("Wells, Ian D", slug: FindSlug("Wells"))
        };
    }
}