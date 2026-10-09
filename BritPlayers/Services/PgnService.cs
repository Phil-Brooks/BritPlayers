using BritPlayers.Models;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace BritPlayers.Services;

public class PgnService
{
    private readonly IWebHostEnvironment _env;
    private readonly ConcurrentDictionary<string, List<ChessGame>> _fileCache = new();
    private bool _isLoaded = false;
    private readonly object _lock = new();

    public PgnService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IReadOnlyList<ChessGame> GetGamesByFile(string fileName)
    {
        return _fileCache.GetOrAdd(fileName, file =>
        {
            var filePath = Path.Combine(_env.ContentRootPath, "Data", "pgn", file);
            if (!File.Exists(filePath))
            {
                return new List<ChessGame>();
            }

            return ParsePgnFile(filePath); // Use your existing PGN parsing method
        });
    }

    public ChessGame? GetGameById(string gameId)
    {
        // Search across loaded games by ID
        foreach (var gameList in _fileCache.Values)
        {
            var match = gameList.FirstOrDefault(g => g.Id == gameId);
            if (match != null) return match;
        }

        return null;
    }

    private static List<ChessGame> ParsePgnFile(string filePath)
    {
        var result = new List<ChessGame>();
        var fileName = Path.GetFileName(filePath);

        if (!File.Exists(filePath)) return result;

        var lines = File.ReadAllLines(filePath, Encoding.UTF8);
        ChessGame? currentGame = null;
        var movesBuilder = new StringBuilder();
        var tagRegex = new Regex(@"^\[(\w+)\s+""(.*)""\]$", RegexOptions.Compiled);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip empty lines
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var match = tagRegex.Match(line);
            if (match.Success)
            {
                var key = match.Groups[1].Value;
                var val = match.Groups[2].Value;

                if (key.Equals("Event", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentGame != null)
                    {
                        currentGame.Moves = CleanMoves(movesBuilder.ToString());
                        if (string.IsNullOrWhiteSpace(currentGame.Id))
                        {
                            currentGame.Id = ChessGame.CreateStableId(
                                currentGame.SourceFile,
                                currentGame.Event,
                                currentGame.Site,
                                currentGame.Date,
                                currentGame.Round,
                                currentGame.White,
                                currentGame.Black,
                                currentGame.Result,
                                currentGame.Eco);
                        }

                        result.Add(currentGame);
                        movesBuilder.Clear();
                    }

                    currentGame = new ChessGame { SourceFile = fileName, Event = val };
                    continue;
                }

                if (currentGame == null)
                {
                    currentGame = new ChessGame { SourceFile = fileName };
                }

                switch (key.ToLowerInvariant())
                {
                    case "site": currentGame.Site = val; break;
                    case "date": currentGame.Date = val; break;
                    case "round": currentGame.Round = val; break;
                    case "white": currentGame.White = val; break;
                    case "black": currentGame.Black = val; break;
                    case "result": currentGame.Result = val; break;
                    case "eco": currentGame.Eco = val; break;
                }
            }
            else
            {
                // This is move text (e.g., 1. e4 e5 ...)
                if (currentGame != null)
                {
                    movesBuilder.Append(' ').Append(line);
                }
            }
        }

        if (currentGame != null)
        {
            currentGame.Moves = CleanMoves(movesBuilder.ToString());
            currentGame.Id = string.IsNullOrWhiteSpace(currentGame.Id)
                ? ChessGame.CreateStableId(
                    currentGame.SourceFile,
                    currentGame.Event,
                    currentGame.Site,
                    currentGame.Date,
                    currentGame.Round,
                    currentGame.White,
                    currentGame.Black,
                    currentGame.Result,
                    currentGame.Eco)
                : currentGame.Id;
            result.Add(currentGame);
        }

        return result;
    }

    private static string CleanMoves(string rawMoves)
    {
        return Regex.Replace(rawMoves.Trim(), @"\s+", " ");
    }
}
