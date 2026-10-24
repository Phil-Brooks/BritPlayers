namespace BritPlayers.Services;

using BritPlayers.Models;

public static class PlayerCatalog
{
    public static readonly IReadOnlyList<PlayerEntry> All =
    [
        new("Daniel J King", "daniel-j-king", "Grandmaster", "b. 1963"),
        new("Michael Adams", "michael-adams", "Grandmaster", "b. 1971"),
        new("Julian M Hodgson", "julian-m-hodgson", "Grandmaster", "b. 1963"),
        new("Glenn C Flear", "glenn-c-flear", "Grandmaster", "b. 1959"),
        new("James H Plaskett", "james-h-plaskett", "Grandmaster", "b. 1960"),
        new("Jonathan A Mestel", "jonathan-a-mestel", "Grandmaster", "b. 1957"),
        new("Jon S Speelman", "jon-s-speelman", "Grandmaster", "b. 1956"),
        new("John D M Nunn", "john-d-m-nunn", "Grandmaster", "b. 1955"),
        new("Michael F Stean", "michael-f-stean", "Grandmaster", "b. 1953"),
        new("Raymond D Keene", "raymond-d-keene", "Grandmaster", "b. 1948"),
        new("Murray Chandler", "murray-chandler", "Grandmaster", "b. 1960"),
        new("Paul Littlewood", "paul-littlewood", "International Master", "b. 1956"),
        new("Anthony J Miles", "anthony-j-miles", "Grandmaster", "1955 – 2001"),
        new("Nigel Short", "nigel-short", "Grandmaster", "b. 1965"),
        new("Ian D Wells", "ian-d-wells", "FIDE Master", "1964 – 1982")
    ];

    public static PlayerEntry GetBySlug(string? slug) =>
        All.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}