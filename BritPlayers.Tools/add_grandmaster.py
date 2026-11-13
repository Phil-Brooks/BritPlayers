import argparse
import os
import re
import shutil
import subprocess
import sys
import tempfile
from datetime import date
from pathlib import Path

import chess.pgn

ROOT_DIR = Path(__file__).resolve().parents[1] / "BritPlayers"
SOURCE_PGN = ROOT_DIR / "Data" / "pgn" / "all.pgn"
INPUT_DIR = ROOT_DIR / "Data" / "pgn_in"
SCORED_DIR = ROOT_DIR / "Data" / "pgn_out"
PUBLISHED_DIR = ROOT_DIR / "Data" / "pgn"
BIOS_DIR = ROOT_DIR / "Data" / "bios"
PHOTOS_DIR = ROOT_DIR / "wwwroot" / "photos"
TOOLS_DIR = Path(__file__).resolve().parent
PLAYER_CATALOG = ROOT_DIR / "Services" / "PlayerCatalog.cs"
UPDATE_CATALOG = ROOT_DIR / "Services" / "UpdateCatalog.cs"


def name_parts(name: str) -> tuple[str, list[str]]:
    if "," in name:
        surname, given_names = name.split(",", 1)
        given_tokens = re.findall(r"[^\W_]+", given_names.casefold())
    else:
        tokens = re.findall(r"[^\W_]+", name.casefold())
        if len(tokens) < 2:
            return "", []
        surname, given_tokens = tokens[-1], tokens[:-1]
    surname_tokens = re.findall(r"[^\W_]+", surname.casefold())
    return (surname_tokens[-1] if surname_tokens else ""), given_tokens


def is_player(game_name: str, profile_name: str) -> bool:
    game_surname, game_given = name_parts(game_name)
    profile_surname, profile_given = name_parts(profile_name)
    if not game_surname or game_surname != profile_surname:
        return False
    return any(
        given_name == profile_name_token
        or (len(given_name) == 1 and profile_name_token.startswith(given_name))
        or (len(profile_name_token) == 1 and given_name.startswith(profile_name_token))
        for given_name in game_given
        for profile_name_token in profile_given
    )


def matches_any_player(game: chess.pgn.Game, names: list[str]) -> bool:
    return any(
        is_player(game.headers.get(side, ""), name)
        for side in ("White", "Black")
        for name in names
    )


def count_games(pgn_file: Path) -> int:
    count = 0
    with open(pgn_file, encoding="utf-8", errors="replace") as source:
        while chess.pgn.read_game(source) is not None:
            count += 1
    return count


def extract_games(source_file: Path, output_file: Path, names: list[str]) -> int:
    if not source_file.is_file():
        raise FileNotFoundError(f"Source PGN does not exist: {source_file}")

    output_file.parent.mkdir(parents=True, exist_ok=True)
    temp_file = None
    games_found = 0
    try:
        with tempfile.NamedTemporaryFile(
            mode="w", encoding="utf-8", newline="\n", dir=output_file.parent,
            suffix=".tmp", delete=False,
        ) as destination:
            temp_file = Path(destination.name)
            with open(source_file, encoding="utf-8", errors="replace") as source:
                games_read = 0
                while True:
                    game = chess.pgn.read_game(source)
                    if game is None:
                        break
                    games_read += 1
                    if matches_any_player(game, names):
                        destination.write(str(game))
                        destination.write("\n\n")
                        games_found += 1
                    if games_read % 10000 == 0:
                        print(f"Scanned {games_read:,} games; found {games_found:,}...", flush=True)

        if games_found == 0:
            raise ValueError(f"No games found for {', '.join(names)} in {source_file}")
        os.replace(temp_file, output_file)
        return games_found
    finally:
        if temp_file is not None and temp_file.exists():
            temp_file.unlink()


def csharp_string(value: str) -> str:
    return value.replace("\\", "\\\\").replace('"', '\\"')


def write_text_atomically(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temp_file = None
    try:
        with tempfile.NamedTemporaryFile(
            mode="w", encoding="utf-8", newline="\n", dir=path.parent,
            suffix=".tmp", delete=False,
        ) as output:
            temp_file = Path(output.name)
            output.write(content)
        os.replace(temp_file, path)
    finally:
        if temp_file is not None and temp_file.exists():
            temp_file.unlink()


def add_catalog_entries(name: str, slug: str, title: str, dates: str, game_count: int) -> None:
    players = PLAYER_CATALOG.read_text(encoding="utf-8")
    updates = UPDATE_CATALOG.read_text(encoding="utf-8")
    player_exists = f'"{csharp_string(slug)}"' in players
    if player_exists and f'"{csharp_string(name)}"' not in players:
        raise ValueError(f"Slug {slug!r} is already used by another player")
    if not player_exists and f'"{csharp_string(name)}"' in players:
        raise ValueError(f"Player {name!r} is already in PlayerCatalog under another slug")

    player_marker = "    public static readonly IReadOnlyList<PlayerEntry> All =\n    [\n"
    player_entry = (
        f'        new("{csharp_string(name)}", "{csharp_string(slug)}", '
        f'"{csharp_string(title)}", "{csharp_string(dates)}"),\n'
    )
    if not player_exists:
        if players.count(player_marker) != 1:
            raise ValueError("Could not find the expected PlayerCatalog insertion point")
        players = players.replace(player_marker, player_marker + player_entry, 1)

    today = date.today()
    update_entry = (
        f'        new(new DateOnly({today.year}, {today.month}, {today.day}), '
        f'"{csharp_string(name)}", UpdateType.AddedPlayer, '
        f'"Player added with {game_count} games"),\n'
    )
    update_exists = (
        f'"{csharp_string(name)}", UpdateType.AddedPlayer' in updates
    )
    update_marker = "    public static readonly IReadOnlyList<UpdateEntry> All =\n    [\n"
    if not update_exists:
        if updates.count(update_marker) != 1:
            raise ValueError("Could not find the expected UpdateCatalog insertion point")
        updates = updates.replace(update_marker, update_marker + update_entry, 1)

    if not player_exists:
        write_text_atomically(PLAYER_CATALOG, players)
    if not update_exists:
        write_text_atomically(UPDATE_CATALOG, updates)


def bio_template(name: str) -> str:
    return (
        f"# {name}\n\n"
        f"**{name}** is a British chess Grandmaster. Add a concise, sourced introduction here.\n\n"
        "## Career Overview\n\n"
        "Add verified career highlights, title and championship information.\n\n"
        "## International Competition\n\n"
        "Add notable team and individual results.\n\n"
        "## Playing Style and Publications\n\n"
        "Add sourced information about playing style and publications.\n"
    )


def publish_file(source: Path, destination: Path, overwrite: bool) -> None:
    if destination.exists():
        if not overwrite:
            import filecmp
            if filecmp.cmp(source, destination, shallow=False):
                return
            raise FileExistsError(
                f"Refusing to overwrite existing file: {destination} (use --overwrite-pgn)"
            )
    destination.parent.mkdir(parents=True, exist_ok=True)
    temp_file = None
    try:
        with tempfile.NamedTemporaryFile(
            mode="wb", dir=destination.parent, suffix=".tmp", delete=False
        ) as output:
            temp_file = Path(output.name)
            with open(source, "rb") as input_file:
                shutil.copyfileobj(input_file, output)
        os.replace(temp_file, destination)
    finally:
        if temp_file is not None and temp_file.exists():
            temp_file.unlink()


def run_workflow(args: argparse.Namespace) -> int:
    input_pgn = INPUT_DIR / f"{args.slug}.pgn"
    scored_pgn = SCORED_DIR / f"{args.slug}.pgn"
    published_pgn = PUBLISHED_DIR / f"{args.slug}.pgn"
    bio_file = BIOS_DIR / f"{args.slug}.md"
    photo_file = PHOTOS_DIR / f"{args.slug}.jpg"
    match_names = [args.name, *args.match_name]

    if args.photo:
        if not args.photo.is_file():
            raise FileNotFoundError(f"Photo file does not exist: {args.photo}")
        if args.photo.suffix.casefold() not in (".jpg", ".jpeg"):
            raise ValueError("Photo must be a JPEG file")
        with open(args.photo, "rb") as photo:
            if photo.read(3) != b"\xff\xd8\xff":
                raise ValueError(f"Photo does not have a JPEG signature: {args.photo}")
        if photo_file.exists() and not args.overwrite_photo:
            raise FileExistsError(
                f"Refusing to overwrite existing photo: {photo_file} (use --overwrite-photo)"
            )

    if input_pgn.exists() and not args.re_extract:
        print(f"Reusing extracted PGN: {input_pgn}")
    else:
        print(f"Extracting matching games from {args.source_pgn} to {input_pgn}...")
        extracted_count = extract_games(args.source_pgn, input_pgn, match_names)
        print(f"Extracted {extracted_count:,} games.")

    scorer = [
        sys.executable,
        str(TOOLS_DIR / "score_pgns.py"),
        "--input-file", str(input_pgn),
        "--output-file", str(scored_pgn),
    ]
    if args.stockfish:
        scorer.extend(["--stockfish", str(args.stockfish)])
    if args.restart_score:
        scorer.append("--restart")
    print("Scoring extracted games. Press Ctrl+C to stop; rerun this command to resume.")
    result = subprocess.run(scorer, check=False)
    if result.returncode == 130:
        print("Scoring interrupted. Rerun this command to resume.")
        return 130
    if result.returncode != 0:
        raise RuntimeError(f"Scoring failed (exit code {result.returncode})")

    game_count = count_games(scored_pgn)
    if game_count == 0:
        raise ValueError(f"Scored PGN contains no games: {scored_pgn}")
    publish_file(scored_pgn, published_pgn, args.overwrite_pgn)

    if not bio_file.exists():
        write_text_atomically(bio_file, bio_template(args.name))
    if args.photo:
        publish_file(args.photo, photo_file, args.overwrite_photo)

    add_catalog_entries(args.name, args.slug, args.title, args.dates, game_count)
    print(
        f"Added {args.name}: {game_count:,} games published to {published_pgn}. "
        f"Review the bio template at {bio_file}."
    )
    if not photo_file.exists():
        print(f"No photo supplied; add a JPEG at {photo_file} when available.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Extract, score, and register a British chess grandmaster."
    )
    parser.add_argument("--name", required=True, help="Display name, e.g. 'John A Smith'")
    parser.add_argument("--slug", required=True, help="Lowercase URL/file slug, e.g. john-a-smith")
    parser.add_argument("--dates", required=True, help="Catalog dates, e.g. 'b. 1970'")
    parser.add_argument("--title", default="Grandmaster")
    parser.add_argument("--match-name", action="append", default=[], help="Additional PGN name/alias; may be repeated")
    parser.add_argument("--source-pgn", type=Path, default=SOURCE_PGN)
    parser.add_argument("--stockfish", type=Path, help="Stockfish executable; otherwise the scorer's default is used")
    parser.add_argument("--photo", type=Path, help="Optional JPEG portrait to copy into wwwroot/photos")
    parser.add_argument("--re-extract", action="store_true", help="Rescan all.pgn and replace the staged extracted PGN")
    parser.add_argument("--restart-score", action="store_true", help="Discard scoring checkpoint and start scoring again")
    parser.add_argument("--overwrite-pgn", action="store_true", help="Allow replacing an existing published player PGN")
    parser.add_argument("--overwrite-photo", action="store_true", help="Allow replacing an existing player JPEG")
    args = parser.parse_args()

    if not re.fullmatch(r"[a-z0-9]+(?:-[a-z0-9]+)*", args.slug):
        parser.error("--slug must contain lowercase letters, digits, and single hyphens")
    if len(re.findall(r"[^\W_]+", args.name)) < 2:
        parser.error("--name must include at least a given name and surname")
    if any(char in value for value in (args.name, args.title, args.dates) for char in "\r\n"):
        parser.error("--name, --title, and --dates must each be a single line")
    if not args.dates.strip() or '"' in args.dates:
        parser.error("--dates must be a single non-empty catalog value without quotes")

    try:
        return run_workflow(args)
    except KeyboardInterrupt:
        print("\nWorkflow interrupted. Rerun the same command to resume scoring.")
        return 130
    except (OSError, ValueError, RuntimeError) as error:
        print(f"Error: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
