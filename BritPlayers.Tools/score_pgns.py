import os
from pathlib import Path
from typing import Optional
import chess
import chess.engine
import chess.pgn

# Paths
STOCKFISH_PATH = r"D:\scid\stockfish-windows-x86-64-universal\stockfish\stockfish-windows-x86-64-universal.exe"  # Update this path!
PGN_DIR = Path(r"D:\Github\BritPlayers\BritPlayers\Data\pgn_in")
OUTPUT_DIR = Path(r"D:\Github\BritPlayers\BritPlayers\Data\pgn_out")

# Engine Settings
EVAL_DEPTH = 15
THREADS = 12
HASH_MB = 256

# Optional time cap per move (in seconds) to prevent infinite search extensions on deep endgames
# Set to None if you strictly want depth 15 only, or e.g. 1.0 to guarantee fast progression.
EVAL_TIME_LIMIT: Optional[float] = 4.0

# Game range selection (1-indexed, inclusive).
START_GAME: Optional[int] = 101
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
    if EVAL_TIME_LIMIT is not None:
        limit = chess.engine.Limit(depth=EVAL_DEPTH, time=EVAL_TIME_LIMIT)
    else:
        limit = chess.engine.Limit(depth=EVAL_DEPTH)

    return engine.analyse(board, limit)


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


def process_file(
    pgn_file: Path,
    engine: chess.engine.SimpleEngine,
    start_game: Optional[int] = START_GAME,
    end_game: Optional[int] = END_GAME,
):
    output_file = OUTPUT_DIR / pgn_file.name
    print(f"\nProcessing {pgn_file.name} -> {output_file.name}...")

    total_games = 0
    annotated_count = 0

    with open(pgn_file, encoding="utf-8", errors="replace") as fin, open(
        output_file, "w", encoding="utf-8"
    ) as fout:

        while True:
            game = chess.pgn.read_game(fin)
            if game is None:
                break

            total_games += 1
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
                output_game = annotate_game(game, engine)
            else:
                output_game = game

            print(output_game, file=fout, end="\n\n")

    print(
        f"\nFinished {pgn_file.name}: {total_games} total games written ({annotated_count} annotated)."
    )


def main():
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    print(f"Starting Stockfish from: {STOCKFISH_PATH}")
    with chess.engine.SimpleEngine.popen_uci(STOCKFISH_PATH) as engine:
        # Crucial options for multi-threaded analysis:
        # UCI_AnalyseMode ensures Stockfish does not stall on time management heuristics.
        engine_options = {
            "Threads": THREADS,
            "Hash": HASH_MB,
        }
        # Set UCI_AnalyseMode if supported by Stockfish
        if "UCI_AnalyseMode" in engine.options:
            engine_options["UCI_AnalyseMode"] = True

        engine.configure(engine_options)

        for pgn_file in PGN_DIR.glob("*.pgn"):
            process_file(
                pgn_file, engine, start_game=START_GAME, end_game=END_GAME
            )

    print("\nAll files successfully evaluated!")


if __name__ == "__main__":
    main()