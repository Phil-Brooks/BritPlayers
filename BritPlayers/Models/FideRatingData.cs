namespace BritPlayers.Models;

public sealed class FideRatingData
{
    public string Source { get; init; } = string.Empty;
    public DateOnly AsOf { get; init; }
    public Dictionary<string, FideRatingEntry> Players { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record FideRatingEntry(int FideId, int Standard);
