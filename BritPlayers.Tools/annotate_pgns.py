import os
import chess
import chess.pgn
import chess.engine
from pathlib import Path

# Paths
STOCKFISH_PATH = r"D:\scid\stockfish-windows-x86-64-universal\stockfish\stockfish-windows-x86-64-universal.exe" # Update this path!
PGN_DIR = Path(r"D:\Github\BritPlayers\BritPlayers\Data\pgn")
OUTPUT_DIR = Path(r"D:\Github\BritPlayers\BritPlayers\Data\pgn_annotated")

# Engine settings: depth 10-12 gives fast, high-quality evals (~0.02s per move)
EVAL_DEPTH = 12
EVAL_TIME_LIMIT = 0.05  # max seconds per move

def format_eval(score: chess.engine.PovScore) -> str:
    """Formats centipawn / mate scores into readable strings: +0.45 or #-3."""
    score_white = score.white()
    if score_white.is_mate():
        mate_in = score_white.mate()
        return f"#{mate_in}"
    cp = score_white.score()
    return f"{cp / 100:+.2f}"

def annotate_game(game: chess.pgn.Game, engine: chess.engine.SimpleEngine) -> chess.pgn.Game:
    board = game.board()
    node = game

    for move in game.mainline_moves():
        node = node.variation(move)
        board.push(move)

        # Analyze current position
        info = engine.analyse(board, chess.engine.Limit(depth=EVAL_DEPTH, time=EVAL_TIME_LIMIT))
        eval_str = format_eval(info["score"])

        # Append or merge standard [%eval ...] tag into comments
        existing_comment = node.comment.strip()
        eval_tag = f"[%eval {eval_str}]"
        
        if "[%eval" not in existing_comment:
            node.comment = f"{existing_comment} {eval_tag}".strip()

    return game

def process_file(pgn_file: Path, engine: chess.engine.SimpleEngine):
    output_file = OUTPUT_DIR / pgn_file.name
    print(f"\nProcessing {pgn_file.name} -> {output_file.name}...")

    games_processed = 0
    with open(pgn_file, encoding="utf-8", errors="replace") as fin, \
         open(output_file, "w", encoding="utf-8") as fout:

        while True:
            game = chess.pgn.read_game(fin)
            if game is None:
                break

            white = game.headers.get("White", "?")
            black = game.headers.get("Black", "?")
            games_processed += 1
            print(f"  [{games_processed}] Analyzing: {white} vs {black}...", end="\r")

            annotated_game = annotate_game(game, engine)
            print(annotated_game, file=fout, end="\n\n")

    print(f"\nFinished {pgn_file.name} ({games_processed} games).")

def main():
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    print(f"Starting Stockfish from: {STOCKFISH_PATH}")
    with chess.engine.SimpleEngine.popen_uci(STOCKFISH_PATH) as engine:
        # Allocate multiple CPU threads if available
        engine.configure({"Threads": 4, "Hash": 128})

        for pgn_file in PGN_DIR.glob("*.pgn"):
            process_file(pgn_file, engine)

    print("\nAll files successfully evaluated!")

if __name__ == "__main__":
    main()
