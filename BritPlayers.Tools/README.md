# BritPlayers data tools

## Add a grandmaster

Export each player's games from Scid vs PC to `BritPlayers\Data\pgn_in\<slug>.pgn`. The PGN filename must match the player's slug, for example `Data\pgn_in\john-m-emms.pgn`. The onboarding tool checks that every game includes the player's name, then scores the games into `Data\pgn_out`, publishes the finished PGN to `Data\pgn`, creates a starter bio in `Data\bios`, copies an optional supplied JPEG into `wwwroot\photos`, and adds the player to `PlayerCatalog` and `UpdateCatalog`. It never reads or scans `all.pgn`.

Run from the repository root:

```powershell
python .\BritPlayers.Tools\add_grandmaster.py --name "John M Emms" --slug john-m-emms --dates "b. 1967" --stockfish "C:\path\to\stockfish.exe"
```

If scoring is interrupted, run the same command again. The scorer resumes from its last completed game using `.partial` and `.checkpoint.json` files in `Data\pgn_out`. Press Ctrl+C to stop after the current engine search; any unfinished game is retried on resume. Use `--restart-score` to discard the scoring checkpoint and start from game one. The bio starter is generic and needs researched content; only use a photo you have permission to publish.

Two editable Windows batch files are included:

- `add_grandmaster.bat` runs scoring and the remaining player setup. Edit the player name, slug, dates, and Stockfish path at the top. It uses `Data\pgn_in\<slug>.pgn` automatically. Add `--photo "C:\path\to\licensed-photo.jpg"` to its Python command if needed.
- `score_grandmaster.bat` scores only the staged PGN for the slug set at the top. It resumes when rerun. After scoring finishes, run `add_grandmaster.bat` (with the same player settings) to publish the PGN and update the catalogs.

To score directly without running the catalog workflow:

```powershell
python .\BritPlayers.Tools\score_pgns.py --input-file .\BritPlayers\Data\pgn_in\john-m-emms.pgn --output-file .\BritPlayers\Data\pgn_out\john-m-emms.pgn --stockfish "C:\path\to\stockfish.exe"
```
