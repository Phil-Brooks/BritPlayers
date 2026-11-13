import argparse
import json
import os
import signal
from pathlib import Path
from typing import Optional
import chess
import chess.engine
import chess.pgn

STOP_REQUESTED = False


class StopRequested(Exception):
    pass


def request_stop(signum, frame) -> None:
    global STOP_REQUESTED
    STOP_REQUESTED = True


ROOT_DIR = Path(__file__).resolve().parents[1] / "BritPlayers"
DEFAULT_STOCKFISH_PATH = os.environ.get(
    "STOCKFISH_PATH",
    r"D:\scid\stockfish-windows-x86-64-universal\stockfish\stockfish-windows-x86-64-universal.exe",
)
PGN_DIR = ROOT_DIR / "Data" / "pgn_in"
OUTPUT_DIR = ROOT_DIR / "Data" / "pgn_out"

# Engine Settings
EVAL_DEPTH = 15
THREADS = 12
HASH_MB = 256

# Optional time cap per move (in seconds) to prevent infinite search extensions on deep endgames
# Set to None if you strictly want depth 15 only, or e.g. 1.0 to guarantee fast progression.
EVAL_TIME_LIMIT: Optional[float] = 4.0

# Game range selection (1-indexed, inclusive).
START_GAME: Optional[int] = 1
END_GAME: Optional[int] = None

# Annotation Settings
MAX_PV_PLIES = 10
INACCURACY_THRESHOLD_CP = 100  # >= 1.00 pawn drop -> $6 (?!)
BLUNDER_THRESHOLD_CP = 300  # >= 3.00 pawn drop -> $4 (??)
CUTOFF_THRESHOLD_CP = 500  # Skip adding variations if already winning/losing by >= 5.00 pawns

# NAG Constants
NAG_BLUNDER = 4  # ?? Blunder
NAG_DUBIOUS = 6  # ?! Inaccuracy
NAG_EQUAL = 10  # = Equal
NAG_WHITE_SLIGHT = 14  # += Slight advantage White
NAG_BLACK_SLIGHT = 15  # =+ Slight advantage Black
NAG_WHITE_MODERATE = 16  # +/- Moderate advantage White
NAG_BLACK_MODERATE = 17  # -/+ Moderate advantage Black
NAG_WHITE_DECISIVE = 18  # +- Decisive advantage White
NAG_BLACK_DECISIVE = 19  # -+ Decisive advantage Black

MATE_SCORE_CP = 30000


def analyze_position(
    board: chess.Board, engine: chess.engine.SimpleEngine
) -> dict:
    """Analyzes position at fixed depth (with optional safety time limit)."""
    if STOP_REQUESTED:
        raise StopRequested
    if EVAL_TIME_LIMIT is not None:
        limit = chess.engine.Limit(depth=EVAL_DEPTH, time=EVAL_TIME_LIMIT)
    else:
        limit = chess.engine.Limit(depth=EVAL_DEPTH)

    info = engine.analyse(board, limit)
    if STOP_REQUESTED:
        raise StopRequested
    return info


def format_eval(score: chess.engine.PovScore, board: chess.Board) -> str:
    """Formats centipawn / mate scores: +0.45, #+2, #-1, #0."""
    if board.is_checkmate():
        return "#-0" if board.turn == chess.WHITE else "#+0"

    score_white = score.white()
    if score_white.is_mate():
        mate_in = score_white.mate()
        return f"#{mate_in:+d}"

    cp = score_white.score()
    if cp is None:
        return "+0.00"
    return f"{cp / 100:+.2f}"


def score_to_cp(score: chess.engine.PovScore) -> int:
    """Returns White-perspective centipawn score for blunder delta calculations."""
    return score.white().score(mate_score=MATE_SCORE_CP)


def get_position_nag(score_white_cp: int) -> int:
    """Returns standard PGN positional NAG based on centipawn score."""
    if score_white_cp >= 300:
        return NAG_WHITE_DECISIVE
    elif score_white_cp >= 150:
        return NAG_WHITE_MODERATE
    elif score_white_cp >= 40:
        return NAG_WHITE_SLIGHT
    elif score_white_cp <= -300:
        return NAG_BLACK_DECISIVE
    elif score_white_cp <= -150:
        return NAG_BLACK_MODERATE
    elif score_white_cp <= -40:
        return NAG_BLACK_SLIGHT
    else:
        return NAG_EQUAL


def annotate_game(
    game: chess.pgn.Game, engine: chess.engine.SimpleEngine
) -> chess.pgn.Game:
    annotator_name = engine.id.get("name", "Stockfish")
    game.headers["Annotator"] = annotator_name
    game.headers["Depth"] = str(EVAL_DEPTH)

    board = game.board()
    node = game

    # Evaluate initial board position before move 1
    prev_info = analyze_position(board, engine)
    prev_cp = score_to_cp(prev_info["score"])
    prev_eval_str = format_eval(prev_info["score"], board)

    for move in list(game.mainline_moves()):
        if STOP_REQUESTED:
            raise StopRequested
        side_to_move = board.turn
        best_move = (
            prev_info["pv"][0] if ("pv" in prev_info and prev_info["pv"]) else None
        )
        pv_line = prev_info.get("pv", [])

        # Step forward
        parent_node = node
        node = parent_node.variation(move)
        board.push(move)

        # Handle terminal positions
        if board.is_checkmate():
            mate_cp = (
                MATE_SCORE_CP if side_to_move == chess.WHITE else -MATE_SCORE_CP
            )
            curr_score = chess.engine.PovScore(
                chess.engine.Mate(0 if side_to_move == chess.WHITE else -0),
                chess.WHITE,
            )
            curr_info = {"score": curr_score, "pv": []}
            curr_cp = mate_cp
        elif board.is_stalemate() or board.is_insufficient_material():
            curr_score = chess.engine.PovScore(chess.engine.Cp(0), chess.WHITE)
            curr_info = {"score": curr_score, "pv": []}
            curr_cp = 0
        else:
            curr_info = analyze_position(board, engine)
            curr_cp = score_to_cp(curr_info["score"])

        curr_eval_str = format_eval(curr_info["score"], board)

        # Evaluation drop from the perspective of the moving player
        if side_to_move == chess.WHITE:
            cp_drop = prev_cp - curr_cp
        else:
            cp_drop = curr_cp - prev_cp

        # Check for blunder/mistake
        is_blunder = (
            best_move is not None
            and move != best_move
            and cp_drop >= INACCURACY_THRESHOLD_CP
            and abs(prev_cp) < CUTOFF_THRESHOLD_CP
        )

        existing_comment = node.comment.strip()

        if is_blunder:
            # 1. Move assessment NAG
            if cp_drop >= BLUNDER_THRESHOLD_CP:
                node.nags.add(NAG_BLUNDER)
            else:
                node.nags.add(NAG_DUBIOUS)

            # 2. Position assessment NAG
            node.nags.add(get_position_nag(curr_cp))

            # 3. Score comment: {played / best}
            score_comment = f"{curr_eval_str} / {prev_eval_str}"
            if existing_comment:
                node.comment = f"{score_comment} {existing_comment}".strip()
            else:
                node.comment = score_comment

            # 4. Variation line with best play
            if pv_line:
                var_board = board.copy()
                var_board.pop()

                var_curr_node = parent_node
                first_var_move = pv_line[0]

                var_curr_node = var_curr_node.add_variation(first_var_move)
                var_board.push(first_var_move)

                for pv_m in pv_line[1:MAX_PV_PLIES]:
                    if var_board.is_legal(pv_m):
                        var_curr_node = var_curr_node.add_variation(pv_m)
                        var_board.push(pv_m)
                    else:
                        break

                var_curr_node.nags.add(get_position_nag(prev_cp))

        else:
            # Normal move
            eval_tag = f"[%eval {curr_eval_str}]"
            if "[%eval" not in existing_comment:
                if existing_comment:
                    node.comment = f"{eval_tag} {existing_comment}".strip()
                else:
                    node.comment = eval_tag

        prev_info = curr_info
        prev_cp = curr_cp
        prev_eval_str = curr_eval_str

    return game


def save_state(state_file: Path, state: dict) -> None:
    temp_file = state_file.with_suffix(state_file.suffix + ".tmp")
    with open(temp_file, "w", encoding="utf-8") as file:
        json.dump(state, file, indent=2)
        file.flush()
        os.fsync(file.fileno())
    os.replace(temp_file, state_file)


def processing_state(
    pgn_file: Path,
    start_game: Optional[int],
    end_game: Optional[int],
) -> dict:
    source_stat = pgn_file.stat()
    return {
        "input": str(pgn_file.resolve()),
        "input_size": source_stat.st_size,
        "input_mtime_ns": source_stat.st_mtime_ns,
        "start_game": start_game,
        "end_game": end_game,
        "depth": EVAL_DEPTH,
        "time_limit": EVAL_TIME_LIMIT,
        "threads": THREADS,
        "hash_mb": HASH_MB,
        "annotation_version": 1,
        "max_pv_plies": MAX_PV_PLIES,
        "inaccuracy_threshold_cp": INACCURACY_THRESHOLD_CP,
        "blunder_threshold_cp": BLUNDER_THRESHOLD_CP,
        "cutoff_threshold_cp": CUTOFF_THRESHOLD_CP,
    }


def process_file(
    pgn_file: Path,
    output_file: Path,
    engine: chess.engine.SimpleEngine,
    start_game: Optional[int] = START_GAME,
    end_game: Optional[int] = END_GAME,
    restart: bool = False,
) -> bool:
    if pgn_file.resolve() == output_file.resolve():
        raise ValueError("Input and output PGN paths must be different")

    output_file.parent.mkdir(parents=True, exist_ok=True)
    partial_file = output_file.with_suffix(output_file.suffix + ".partial")
    state_file = output_file.with_suffix(output_file.suffix + ".checkpoint.json")
    current_state = processing_state(pgn_file, start_game, end_game)

    if restart:
        partial_file.unlink(missing_ok=True)
        state_file.unlink(missing_ok=True)

    completed_games = 0
    output_bytes = 0
    if state_file.exists():
        with open(state_file, encoding="utf-8") as file:
            saved_state = json.load(file)
        if any(saved_state.get(key) != value for key, value in current_state.items()):
            raise ValueError(
                f"Checkpoint does not match {pgn_file}. Use --restart to start over."
            )
        if (
            saved_state.get("complete")
            and output_file.exists()
            and output_file.stat().st_size == saved_state.get("output_bytes")
        ):
            print(f"Already scored: {output_file}")
            return True
        if partial_file.exists():
            completed_games = saved_state.get("completed_games", 0)
            output_bytes = saved_state.get("output_bytes", 0)
            if (
                not isinstance(completed_games, int)
                or completed_games < 0
                or not isinstance(output_bytes, int)
                or output_bytes < 0
                or output_bytes > partial_file.stat().st_size
            ):
                raise ValueError(f"Invalid checkpoint for {pgn_file}. Use --restart to start over.")
        else:
            print(f"Checkpoint output missing; restarting {pgn_file.name} from game 1.")
    elif partial_file.exists():
        partial_file.unlink()

    print(f"\nProcessing {pgn_file.name} -> {output_file.name}...")
    annotated_count = 0
    total_games = 0

    with open(pgn_file, encoding="utf-8", errors="replace") as fin, open(
        partial_file, "r+b" if partial_file.exists() else "w+b"
    ) as fout:
        fout.truncate(output_bytes)
        fout.seek(output_bytes)

        while True:
            game = chess.pgn.read_game(fin)
            if game is None:
                break

            total_games += 1
            if total_games <= completed_games:
                continue

            white = game.headers.get("White", "?")
            black = game.headers.get("Black", "?")
            in_range = (start_game is None or total_games >= start_game) and (
                end_game is None or total_games <= end_game
            )

            if in_range:
                annotated_count += 1
                print(
                    f"  [Game {total_games}] Analyzing: {white} vs {black}...",
                    end="\r",
                )
                try:
                    output_game = annotate_game(game, engine)
                except StopRequested:
                    print("\nScoring stopped; the current game will be retried on resume.")
                    return False
            else:
                output_game = game

            fout.write((str(output_game) + "\n\n").encode("utf-8"))
            fout.flush()
            os.fsync(fout.fileno())
            current_checkpoint = {
                **current_state,
                "complete": False,
                "completed_games": total_games,
                "output_bytes": fout.tell(),
            }
            save_state(state_file, current_checkpoint)
            if STOP_REQUESTED:
                print("\nScoring stopped at a completed-game checkpoint.")
                return False

        fout.flush()
        os.fsync(fout.fileno())

    os.replace(partial_file, output_file)
    save_state(state_file, {
        **current_state,
        "complete": True,
        "completed_games": total_games,
        "output_bytes": output_file.stat().st_size,
    })
    print(
        f"\nFinished {pgn_file.name}: {total_games} total games written ({annotated_count} annotated)."
    )
    return True


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Score PGN games with Stockfish; completed games are checkpointed for safe resume."
    )
    parser.add_argument("--input-dir", type=Path, default=PGN_DIR)
    parser.add_argument("--output-dir", type=Path, default=OUTPUT_DIR)
    parser.add_argument("--input-file", type=Path, help="Score one PGN instead of every file in --input-dir")
    parser.add_argument("--output-file", type=Path, help="Output path when --input-file is used")
    parser.add_argument("--stockfish", type=Path, default=Path(DEFAULT_STOCKFISH_PATH))
    parser.add_argument("--start-game", type=int, default=START_GAME)
    parser.add_argument("--end-game", type=int, default=END_GAME)
    parser.add_argument("--restart", action="store_true", help="Discard checkpoint and start scoring from game 1")
    args = parser.parse_args()

    if args.input_file:
        if not args.input_file.is_file():
            parser.error(f"Input PGN does not exist: {args.input_file}")
        pgn_files = [args.input_file]
    else:
        pgn_files = sorted(args.input_dir.glob("*.pgn"))
        if not pgn_files:
            parser.error(f"No PGN files found in {args.input_dir}")

    if args.start_game is not None and args.start_game < 1:
        parser.error("--start-game must be 1 or greater")
    if args.end_game is not None and args.end_game < 1:
        parser.error("--end-game must be 1 or greater")
    if args.input_file and args.output_file is None:
        output_for = lambda source: args.output_dir / source.name
    elif args.input_file:
        output_for = lambda source: args.output_file
    else:
        if args.output_file:
            parser.error("--output-file can only be used with --input-file")
        output_for = lambda source: args.output_dir / source.name

    signal.signal(signal.SIGINT, request_stop)
    try:
        print(f"Starting Stockfish from: {args.stockfish}")
        with chess.engine.SimpleEngine.popen_uci(str(args.stockfish)) as engine:
            engine_options = {"Threads": THREADS, "Hash": HASH_MB}
            if "UCI_AnalyseMode" in engine.options:
                engine_options["UCI_AnalyseMode"] = True
            engine.configure(engine_options)

            for pgn_file in pgn_files:
                if not process_file(
                    pgn_file,
                    output_for(pgn_file),
                    engine,
                    start_game=args.start_game,
                    end_game=args.end_game,
                    restart=args.restart,
                ):
                    print("Run the same command again to resume.")
                    return 130
    except KeyboardInterrupt:
        print("\nStopped. Run the same command again to resume.")
        return 130

    print("\nAll files successfully evaluated!")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())