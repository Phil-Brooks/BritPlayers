import argparse
import io
import os
import re
import tempfile
import urllib.parse
import urllib.request
import zipfile
from collections import Counter, defaultdict
from datetime import date, datetime
from html.parser import HTMLParser
from pathlib import Path
from typing import Optional

ROOT_DIR = Path(__file__).resolve().parents[1] / "BritPlayers"
ARCHIVE_URL = "https://theweekinchess.com/twic"
USER_AGENT = "BritPlayers PGN downloader/1.0 (personal use)"
REQUEST_TIMEOUT = 60


class ArchiveTableParser(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.rows = []
        self._row = None
        self._cell_text = None
        self._cell_links = None

    def handle_starttag(self, tag, attrs):
        if tag == "tr":
            self._row = []
        elif tag in ("td", "th") and self._row is not None:
            self._cell_text = []
            self._cell_links = []
        elif tag == "a" and self._cell_links is not None:
            href = dict(attrs).get("href")
            if href:
                self._cell_links.append(href)

    def handle_data(self, data):
        if self._cell_text is not None:
            self._cell_text.append(data)

    def handle_endtag(self, tag):
        if tag in ("td", "th") and self._row is not None and self._cell_text is not None:
            self._row.append(("".join(self._cell_text).strip(), self._cell_links))
            self._cell_text = None
            self._cell_links = None
        elif tag == "tr" and self._row is not None:
            self.rows.append(self._row)
            self._row = None


def fetch_url(url: str) -> bytes:
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request, timeout=REQUEST_TIMEOUT) as response:
        return response.read()


def parse_issue_date(value: str) -> Optional[date]:
    if not re.fullmatch(r"\d{4}\.\d{2}\.\d{2}", value):
        return None
    try:
        return datetime.strptime(value, "%Y.%m.%d").date()
    except ValueError:
        return None


PGN_GAME_START = re.compile(r"(?m)^\[Event(?:\s|$)")
PGN_HEADER = re.compile(r'^\[([^\s]+)\s+"((?:\\.|[^"])*)"\]\s*$', re.MULTILINE)


def iter_game_records(pgn_text: str):
    normalized_text = pgn_text.replace("\r\n", "\n").replace("\r", "\n")
    starts = list(PGN_GAME_START.finditer(normalized_text))
    if not starts:
        if normalized_text.strip():
            raise ValueError("PGN contains data but no [Event] game headers")
        return

    for index, match in enumerate(starts):
        end = starts[index + 1].start() if index + 1 < len(starts) else len(normalized_text)
        record = normalized_text[match.start():end].strip()
        if record:
            yield record


def game_headers(record: str) -> dict:
    header_text = record.split("\n\n", 1)[0]
    return {
        match.group(1): re.sub(r'\\(["\\])', r"\1", match.group(2))
        for match in PGN_HEADER.finditer(header_text)
    }


def player_key(name: str) -> tuple:
    return tuple(sorted(re.findall(r"[^\W_]+", name.casefold())))


def source_player_metadata(pgn_path: Path) -> tuple:
    latest = None
    player_counts = Counter()
    name_counts = defaultdict(Counter)
    player_ids = defaultdict(set)
    pgn_text = pgn_path.read_text(encoding="utf-8", errors="replace")

    for record in iter_game_records(pgn_text):
        headers = game_headers(record)
        game_date = parse_issue_date(headers.get("Date", ""))
        if game_date is not None and (latest is None or game_date > latest):
            latest = game_date

        for side in ("White", "Black"):
            name = headers.get(side, "")
            key = player_key(name)
            if key:
                player_counts[key] += 1
                name_counts[key][name] += 1
                fide_id = headers.get(f"{side}FideId", "")
                if fide_id:
                    player_ids[key].add(fide_id)

    if latest is None:
        raise ValueError(f"No complete game dates found in {pgn_path}")
    if not player_counts:
        raise ValueError(f"No player names found in {pgn_path}")

    target_player = player_counts.most_common(1)[0][0]
    canonical_name = name_counts[target_player].most_common(1)[0][0]
    return latest, player_ids[target_player], canonical_name


def player_name_matches(name: str, canonical_name: str) -> bool:
    if player_key(name) == player_key(canonical_name):
        return True

    if "," not in name or "," not in canonical_name:
        return False

    surname, given_names = name.split(",", 1)
    source_surname, source_given_names = canonical_name.split(",", 1)
    if player_key(surname) != player_key(source_surname):
        return False

    given_tokens = player_key(given_names)
    source_given_tokens = player_key(source_given_names)
    return bool(given_tokens) and all(
        any(source_token.startswith(token) for source_token in source_given_tokens)
        for token in given_tokens
    )


def game_has_player(headers: dict, player_ids: set, canonical_name: str) -> bool:
    for side in ("White", "Black"):
        name = headers.get(side, "")
        if player_name_matches(name, canonical_name):
            return True
        fide_id = headers.get(f"{side}FideId", "")
        if fide_id and fide_id in player_ids:
            return True
    return False


def normalize_player_name(record: str, headers: dict, player_ids: set, name: str) -> str:
    for side in ("White", "Black"):
        fide_id = headers.get(f"{side}FideId", "")
        if player_name_matches(headers.get(side, ""), name) or (fide_id and fide_id in player_ids):
            record = re.sub(
                rf'(?m)^\[{side} "[^"\n]*"\]$',
                f'[{side} "{name}"]',
                record,
                count=1,
            )
    return record


def game_key(headers: dict, record: str) -> tuple:
    _, separator, moves = record.partition("\n\n")
    return (
        headers.get("Date", ""),
        headers.get("White", ""),
        headers.get("Black", ""),
        headers.get("Event", ""),
        headers.get("Round", ""),
        headers.get("Result", ""),
        re.sub(r"\s+", " ", moves if separator else "").strip(),
    )


def write_games(games_by_key: dict, output_path: Path) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    temp_path = None
    try:
        with tempfile.NamedTemporaryFile(
            mode="w",
            encoding="utf-8",
            newline="",
            dir=output_path.parent,
            suffix=".tmp",
            delete=False,
        ) as output_file:
            temp_path = Path(output_file.name)
            for record in games_by_key.values():
                output_file.write(record)
                output_file.write("\n\n")
        os.replace(temp_path, output_path)
    finally:
        if temp_path is not None and temp_path.exists():
            temp_path.unlink()


def find_issues(archive_html: str) -> list:
    parser = ArchiveTableParser()
    parser.feed(archive_html)
    issues = {}

    for row in parser.rows:
        row_text = " ".join(text for text, _ in row)
        date_match = re.search(r"\b(\d{4}-\d{2}-\d{2})\b", row_text)
        if not date_match:
            continue
        issue_date = datetime.strptime(date_match.group(1), "%Y-%m-%d").date()

        for _, links in row:
            for link in links:
                match = re.search(r"twic(\d+)g\.zip(?:$|[?#])", link, re.IGNORECASE)
                if match:
                    issue_number = int(match.group(1))
                    issues[issue_number] = (
                        issue_date,
                        urllib.parse.urljoin(ARCHIVE_URL, link),
                    )

    if not issues:
        raise ValueError("Could not find downloadable PGN editions on the TWIC archive page")
    return sorted(
        ((number, issue_date, url) for number, (issue_date, url) in issues.items()),
        reverse=True,
    )


def fetch_all_players(source_dir: Path, output_dir: Path) -> None:
    players = {}
    for source_path in sorted(source_dir.glob("*.pgn")):
        cutoff, player_ids, canonical_name = source_player_metadata(source_path)
        players[source_path.name] = {
            "cutoff": cutoff,
            "player_ids": player_ids,
            "canonical_name": canonical_name,
            "games": {},
        }
        print(f"{source_path.name}: {canonical_name}, newer than {cutoff.isoformat()}")

    if not players:
        raise ValueError(f"No PGN files found in {source_dir}")

    archive_html = fetch_url(ARCHIVE_URL).decode("utf-8", errors="replace")
    issues = find_issues(archive_html)
    oldest_cutoff = min(player["cutoff"] for player in players.values())

    for issue_number, issue_date, issue_url in issues:
        if issue_date < oldest_cutoff:
            break
        print(f"Checking TWIC {issue_number} ({issue_date.isoformat()})...")
        archive_data = fetch_url(issue_url)
        with zipfile.ZipFile(io.BytesIO(archive_data)) as archive:
            pgn_names = sorted(
                name for name in archive.namelist() if name.lower().endswith(".pgn")
            )
            if not pgn_names:
                raise ValueError(f"No PGN found in TWIC {issue_number} archive")

            for pgn_name in pgn_names:
                pgn_text = archive.read(pgn_name).decode("utf-8", errors="replace")
                for record in iter_game_records(pgn_text):
                    headers = game_headers(record)
                    game_date = parse_issue_date(headers.get("Date", ""))
                    if game_date is None:
                        continue

                    for filename, player in players.items():
                        if game_date <= player["cutoff"] or not game_has_player(
                            headers, player["player_ids"], player["canonical_name"]
                        ):
                            continue
                        normalized_record = normalize_player_name(
                            record,
                            headers,
                            player["player_ids"],
                            player["canonical_name"],
                        )
                        normalized_headers = game_headers(normalized_record)
                        key = game_key(normalized_headers, normalized_record)
                        player["games"].setdefault(key, normalized_record)

    for filename, player in players.items():
        output_path = output_dir / filename
        write_games(player["games"], output_path)
        print(f"Wrote {len(player['games'])} newer games to {output_path}")


def main():
    parser = argparse.ArgumentParser(
        description="Collect newer games for every player PGN from TWIC."
    )
    parser.add_argument(
        "--source-dir",
        type=Path,
        default=ROOT_DIR / "Data" / "pgn",
        help="Directory containing player PGNs",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=ROOT_DIR / "Data" / "pgn-new",
        help="Directory for the generated PGNs",
    )
    args = parser.parse_args()

    if not args.source_dir.is_dir():
        parser.error(f"Source PGN directory does not exist: {args.source_dir}")
    fetch_all_players(args.source_dir, args.output_dir)


if __name__ == "__main__":
    main()
