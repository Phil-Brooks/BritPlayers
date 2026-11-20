namespace BritPlayers.Models;

public sealed record PlayerEntry(string Name, string Slug, string Title, string Dates)
{
    public string PgnFileName => $"{Slug}.pgn";

    public string TitleAbbreviation => Title switch
    {
        "Grandmaster" => "GM",
        "International Master" => "IM",
        "FIDE Master" => "FM",
        _ => Title
    };
}