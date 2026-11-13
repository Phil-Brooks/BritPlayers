# BritPlayers data tools

## Add a grandmaster

Run from the repository root (or pass the full script path):

```powershell
python .\BritPlayers.Tools\add_grandmaster.py `
  --name "Example Name" `
  --slug example-name `
  --dates "b. 19xx" `
  --match-name "Name, Example" `
  --stockfish "C:\path\to\stockfish.exe" `
  --photo "C:\path\to\licensed-photo.jpg"
```

The command streams `BritPlayers\Data\pgn\all.pgn`, extracts the matching games to `Data\pgn_in`, scores them into `Data\pgn_out`, publishes the scored PGN to `Data\pgn`, creates a starter bio in `Data\bios`, copies an optional supplied JPEG into `wwwroot\photos`, and adds entries to `PlayerCatalog` and `UpdateCatalog`. Add `--match-name` once for each additional name/alias found in the PGN headers. The bio starter is intentionally generic: verify and write the player's biography yourself, and only use a portrait you have permission to publish.

If the command is interrupted during scoring, run the same command again. The extracted PGN is reused, and `score_pgns.py` resumes from its last completed game using its `.partial` and `.checkpoint.json` files in `Data\pgn_out`. Press Ctrl+C to stop after the current engine search; any unfinished game is retried on resume. Use `--restart-score` to discard the checkpoint and start scoring from scratch, or `--re-extract` to rescan `all.pgn`. Use `--overwrite-pgn` or `--overwrite-photo` only when intentionally replacing an existing file.

Two editable Windows batch-file examples are included in this folder:

- `add_grandmaster.bat` runs the full extraction, scoring, publishing, bio-starter, and catalog workflow. Edit the player name, slug, dates, and Stockfish path at the top first. Add `--match-name "Name, Example"` or `--photo "C:\path\to\licensed-photo.jpg"` to its Python command if needed.
- `score_grandmaster.bat` scores only the already-extracted PGN for the slug set at the top. It resumes from the scorer checkpoint when rerun. After standalone scoring finishes, run `add_grandmaster.bat` to complete publishing and catalog updates; the completed score will be reused.

Alternatively, run the scorer directly:

```powershell
python .\BritPlayers.Tools\score_pgns.py --input-file .\BritPlayers\Data\pgn_in\example-name.pgn --output-file .\BritPlayers\Data\pgn_out\example-name.pgn --stockfish "C:\path\to\stockfish.exe"
```
