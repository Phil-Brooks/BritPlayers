using System.Text;
using System.Text.RegularExpressions;
using BritPlayers.Models;

namespace BritPlayers.Services;

public class PgnService
{
    private readonly List<ChessGame> _cachedGames = new();
    private bool _isLoaded;
    private readonly object _lock = new();

    private readonly string[] _candidateRoots = new[]
    {
        Environment.GetEnvironmentVariable("BRITBASE_PGN_DIR"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "BritBase", "BritBase", "Data", "pgn"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BritBase", "BritBase", "Data", "pgn"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BritBase", "Data", "pgn"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "pgn"),
        Path.Combine(AppContext.BaseDirectory, "Data", "pgn"),
        Path.Combine(Environment.CurrentDirectory, "Data", "pgn"),
        Path.Combine(Environment.CurrentDirectory, "..", "BritBase", "Data", "pgn"),
        Path.Combine(Environment.CurrentDirectory, "..", "BritBase", "BritBase", "Data", "pgn"),
        Path.Combine(Environment.CurrentDirectory, "..", "..", "BritBase", "BritBase", "Data", "pgn"),
        Path.Combine(Environment.CurrentDirectory, "..", "..", "BritBase", "Data", "pgn"),
        "D:\\Github\\BritBase\\BritBase\\Data\\pgn"
    }
    .OfType<string>()
    .Where(path => !string.IsNullOrWhiteSpace(path))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

    public IReadOnlyList<ChessGame> GetAllGames()
    {
        if (_isLoaded) return _cachedGames;

        lock (_lock)
        {
            if (_isLoaded) return _cachedGames;

            var pgnDir = GetPgnDirectory();
            if (string.IsNullOrEmpty(pgnDir))
            {
                _isLoaded = true;
                return _cachedGames;
            }

            foreach (var file in Directory.GetFiles(pgnDir, "*.pgn", SearchOption.AllDirectories))
            {
                _cachedGames.AddRange(ParsePgnFile(file));
            }

            _isLoaded = true;
            return _cachedGames;
        }
    }

    public ChessGame? GetGameById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return GetAllGames().FirstOrDefault(g => g.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<ChessGame> SearchByPlayer(string? playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return Array.Empty<ChessGame>();
        }

        var normalized = playerName.Trim();
        return GetAllGames()
            .Where(g =>
                g.White.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                g.Black.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                g.White.Contains(normalized.Replace('-', ' '), StringComparison.OrdinalIgnoreCase) ||
                g.Black.Contains(normalized.Replace('-', ' '), StringComparison.OrdinalIgnoreCase))
            .OrderBy(g => g.Date)
            .ToList();
    }

    public IReadOnlyList<ChessGame> SearchByPlayer(IEnumerable<string> aliases)
    {
        var names = aliases
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
        {
            return Array.Empty<ChessGame>();
        }

        return GetAllGames()
            .Where(g => names.Any(alias =>
                g.White.Contains(alias, StringComparison.OrdinalIgnoreCase) ||
                g.Black.Contains(alias, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(g => g.Date)
            .ToList();
    }

    private string? GetPgnDirectory()
    {
        foreach (var root in _candidateRoots)
        {
            var normalized = Path.GetFullPath(root);
            if (Directory.Exists(normalized))
            {
                return normalized;
            }
        }

        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            var candidates = new[]
            {
                Path.Combine(dir.FullName, "BritBase", "BritBase", "Data", "pgn"),
                Path.Combine(dir.FullName, "BritBase", "Data", "pgn"),
                Path.Combine(dir.FullName, "Data", "pgn")
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            dir = dir.Parent;
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
            if (string.IsNullOrEmpty(line)) continue;

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
            else if (currentGame != null)
            {
                movesBuilder.Append(' ').Append(line);
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
