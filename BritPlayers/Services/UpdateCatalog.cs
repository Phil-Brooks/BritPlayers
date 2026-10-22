namespace BritPlayers.Services;

using BritPlayers.Models;

public static class UpdateCatalog
{
    public static readonly IReadOnlyList<UpdateEntry> All =
    [
        new(new DateOnly(2026, 10, 21), "Julian M Hodgson", UpdateType.AddedPlayer, "Player added with 1960 games"),
        new(new DateOnly(2026, 10, 21), "Glenn C Flear", UpdateType.AddedPlayer, "Player added with 2621 games"),
        new(new DateOnly(2026, 10, 20), "James H Plaskett", UpdateType.AddedPlayer, "Player added with 1492 games"),
        new(new DateOnly(2026, 10, 19), "Jonathan A Mestel", UpdateType.AddedPlayer, "Player added with 1153 games"),
        new(new DateOnly(2026, 10, 18), "Jon S Speelman", UpdateType.AddedPlayer, "Player added with 2836 games"),
        new(new DateOnly(2026, 10, 18), "John D M Nunn", UpdateType.AddedPlayer, "Player added with 2370 games"),
        new(new DateOnly(2026, 10, 16), "Michael F Stean", UpdateType.AddedPlayer, "Player added with 824 games"),
        new(new DateOnly(2026, 10, 15), "Raymond D Keene", UpdateType.AddedPlayer, "Player added with 1257 games"),
        new(new DateOnly(2026, 10, 14), "Paul Littlewood", UpdateType.DeletedGames, "Deleted 11 games which had no moves"),
        new(new DateOnly(2026, 10, 14), "Nigel Short",     UpdateType.DeletedGames, "Deleted 19 games which had no moves"),
        new(new DateOnly(2026, 10, 13), "Anthony J Miles",      UpdateType.DeletedGames, "Deleted 18 games which had no moves"),
        new(new DateOnly(2026, 10, 10), "Murray Chandler", UpdateType.DeletedGames, "Deleted 43 games which had no moves, such as the 1972 game against Thomson, O"),
        new(new DateOnly(2026, 10, 10), "Murray Chandler", UpdateType.ScoreFix,
            "NZL Schoolboys (1974.09.02, Round 2) • Wansink, Robert vs Chandler, Murray G",
            "Corrected game result from 1-0 to 0-1.")
    ];
}