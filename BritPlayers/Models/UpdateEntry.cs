namespace BritPlayers.Models;

public enum UpdateType
{
    AddedPlayer,
    DeletedGames,
    ScoreFix,
    AddedGames,
    Correction
}

public static class UpdateTypeExtensions
{
    public static string DisplayName(this UpdateType type) => type switch
    {
        UpdateType.AddedPlayer => "Added Player",
        UpdateType.DeletedGames => "Deleted Games",
        UpdateType.ScoreFix => "Score Fix",
        UpdateType.AddedGames => "Added Games",
        UpdateType.Correction => "Correction",
        _ => type.ToString()
    };

    public static string BadgeClass(this UpdateType type) => type switch
    {
        UpdateType.AddedPlayer => "bg-primary-subtle text-primary border border-primary-subtle",
        UpdateType.DeletedGames => "bg-danger-subtle text-danger border border-danger-subtle",
        UpdateType.ScoreFix => "bg-warning-subtle text-warning-emphasis border border-warning-subtle",
        UpdateType.AddedGames => "bg-success-subtle text-success border border-success-subtle",
        UpdateType.Correction => "bg-info-subtle text-info-emphasis border border-info-subtle",
        _ => "bg-secondary-subtle text-secondary border"
    };

    public static string IconClass(this UpdateType type) => type switch
    {
        UpdateType.AddedPlayer => "bi-person-plus",
        UpdateType.DeletedGames => "bi-trash3",
        UpdateType.ScoreFix => "bi-arrow-left-right",
        UpdateType.AddedGames => "bi-plus-circle",
        UpdateType.Correction => "bi-pencil",
        _ => "bi-info-circle"
    };
}

public sealed record UpdateEntry(
    DateOnly Date,
    string PlayerName,
    UpdateType Type,
    string Details,
    string? SubDetails = null
);