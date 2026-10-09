namespace BritPlayers.Services;

using BritPlayers.Models;

public static class PlayerCatalog
{
    public static readonly IReadOnlyList<PlayerEntry> All =
    [
        new("Murray Chandler", "murray-chandler", "chandlerm.pgn", "Grandmaster", "b. 1960"),
        new("Paul Littlewood", "paul-littlewood", "littlewoodpaul.pgn", "International Master", "b. 1956"),
        new("Anthony J Miles", "anthony-j-miles", "milestony.pgn", "Grandmaster", "1955 – 2001"),
        new("Nigel Short", "nigel-short", "shortn.pgn", "Grandmaster", "b. 1965"),
        new("Ian D Wells", "ian-d-wells", "wells_ian.pgn", "FIDE Master", "1964 – 1982")
    ];

    public static PlayerEntry GetBySlug(string? slug) =>
        All.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}