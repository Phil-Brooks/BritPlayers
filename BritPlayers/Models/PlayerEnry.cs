namespace BritPlayers.Models;

public sealed record PlayerEntry(string Name, string Slug, string Title, string Dates)
{
    // Automatically computed from the slug!
    public string PgnFileName => $"{Slug}.pgn";
}