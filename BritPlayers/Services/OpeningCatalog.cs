using System.Text.RegularExpressions;

namespace BritPlayers.Services;

public static class OpeningCatalog
{
    private static readonly IReadOnlyDictionary<string, string> NamedCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["A00"] = "Irregular Opening",
        ["A01"] = "Nimzo-Larsen Attack",
        ["A02"] = "Bird's Opening",
        ["A04"] = "Reti Opening",
        ["A07"] = "King's Indian Attack",
        ["A10"] = "English Opening",
        ["A15"] = "English Opening: Anglo-Indian Defense",
        ["A21"] = "English Opening: King's English",
        ["B00"] = "King's Pawn Game",
        ["B01"] = "Scandinavian Defense",
        ["B02"] = "Alekhine Defense",
        ["B06"] = "Robatsch Defense",
        ["B07"] = "Pirc Defense",
        ["B10"] = "Caro-Kann Defense",
        ["B20"] = "Sicilian Defense",
        ["C00"] = "French Defense",
        ["C20"] = "King's Pawn Game",
        ["C30"] = "King's Gambit",
        ["C42"] = "Petrov's Defense",
        ["C45"] = "Scotch Game",
        ["C46"] = "Three Knights Game",
        ["C47"] = "Four Knights Game",
        ["C50"] = "Italian Game",
        ["C54"] = "Italian Game: Classical Variation",
        ["C60"] = "Ruy Lopez",
        ["D00"] = "Queen's Pawn Game",
        ["D30"] = "Queen's Gambit Declined",
        ["D70"] = "Grunfeld Defense",
        ["E00"] = "Catalan Opening",
        ["E10"] = "Queen's Indian Defense",
        ["E20"] = "Nimzo-Indian Defense",
        ["E60"] = "King's Indian Defense"
    };

    public static string GetName(string? eco)
    {
        var match = Regex.Match(eco ?? string.Empty, @"^([A-E]\d{2})", RegexOptions.IgnoreCase);
        if (!match.Success) return "Unknown opening";

        var code = match.Groups[1].Value.ToUpperInvariant();
        if (NamedCodes.TryGetValue(code, out var name)) return name;

        var letter = code[0];
        var number = int.Parse(code.AsSpan(1), System.Globalization.CultureInfo.InvariantCulture);
        return letter switch
        {
            'A' when number <= 0 => "Irregular Opening",
            'A' when number == 1 => "Nimzo-Larsen Attack",
            'A' when number <= 3 => "Bird's Opening",
            'A' when number <= 9 => "Reti Opening",
            'A' when number <= 39 => "English Opening",
            'A' when number <= 49 => "Queen's Pawn and Indian Openings",
            'A' when number <= 79 => "Indian Opening",
            'A' => "Dutch Defense",
            'B' when number == 1 => "Scandinavian Defense",
            'B' when number <= 5 => "Alekhine Defense",
            'B' when number == 6 => "Robatsch Defense",
            'B' when number <= 9 => "Pirc Defense",
            'B' when number <= 19 => "Caro-Kann Defense",
            'B' => "Sicilian Defense",
            'C' when number <= 19 => "French Defense",
            'C' when number <= 29 => "King's Pawn Game",
            'C' when number <= 39 => "King's Gambit",
            'C' when number <= 41 => "King's Knight Opening",
            'C' when number <= 43 => "Petrov's Defense",
            'C' when number == 44 => "Open Game",
            'C' when number == 45 => "Scotch Game",
            'C' when number == 46 => "Three Knights Game",
            'C' when number <= 49 => "Four Knights Game",
            'C' when number <= 59 => "Italian Game",
            'C' => "Ruy Lopez",
            'D' when number <= 5 => "Queen's Pawn Game",
            'D' when number <= 69 => "Queen's Gambit",
            'D' => "Grunfeld Defense",
            'E' when number <= 9 => "Catalan Opening",
            'E' when number <= 19 => "Queen's Indian Defense",
            'E' when number <= 59 => "Nimzo-Indian Defense",
            'E' => "King's Indian Defense",
            _ => "Unknown opening"
        };
    }
}
