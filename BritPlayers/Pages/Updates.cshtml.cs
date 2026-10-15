using Microsoft.AspNetCore.Mvc.RazorPages;
using BritPlayers.Models;
using BritPlayers.Services;

namespace BritPlayers.Pages;

public class UpdatesModel : PageModel
{
    public record UpdateViewModel(UpdateEntry Entry, string? PlayerSlug);

    public IReadOnlyList<UpdateViewModel> Updates { get; private set; } = [];

    public void OnGet()
    {
        Updates = UpdateCatalog.All
            .OrderByDescending(u => u.Date)
            .Select(entry =>
            {
                var slug = PlayerCatalog.All.FirstOrDefault(p =>
                    p.Name.Contains(entry.PlayerName, StringComparison.OrdinalIgnoreCase) ||
                    entry.PlayerName.Contains(p.Name, StringComparison.OrdinalIgnoreCase))?.Slug;

                return new UpdateViewModel(entry, slug);
            })
            .ToList();
    }
}