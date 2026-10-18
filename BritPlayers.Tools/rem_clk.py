import re

# Regex matching [%clk ...] including any preceding/following whitespace
CLK_REGEX = re.compile(r"\s*\[%clk\s+[^\]]+\]")
# Optional cleanup for comments that become empty, e.g. {} or {  }
EMPTY_COMMENT_REGEX = re.compile(r"\{\s*\}")


def remove_clk_annotations(input_file: str, output_file: str):
    with open(input_file, "r", encoding="utf-8", errors="replace") as infile, open(
        output_file, "w", encoding="utf-8"
    ) as outfile:

        # If the file fits in memory (e.g. up to a few hundred MBs),
        # processing all at once handles [%clk ...] tags that span across line breaks.
        content = infile.read()

        # Remove [%clk ...] tags
        content = CLK_REGEX.sub("", content)

        # Clean up any comments that were left completely empty
        content = EMPTY_COMMENT_REGEX.sub("", content)

        outfile.write(content)


if __name__ == "__main__":
    input_pgn = "games.pgn"
    output_pgn = "games_cleaned.pgn"

    print("Cleaning PGN...")
    remove_clk_annotations(input_pgn, output_pgn)
    print(f"Done! Cleaned file saved to {output_pgn}")