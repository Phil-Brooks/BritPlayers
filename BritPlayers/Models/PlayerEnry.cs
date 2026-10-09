namespace BritPlayers.Models;

public sealed record PlayerEntry(
    string Name,
    string Slug,
    string PgnFileName,
    string Title,
    string Dates,
    string BioHtml
);