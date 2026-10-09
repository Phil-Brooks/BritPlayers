namespace BritPlayers.Services;

using BritPlayers.Models;

public static class PlayerCatalog
{
    public static readonly IReadOnlyList<PlayerEntry> All =
    [
        new(
            "Murray Chandler",
            "murray-chandler",
            "chandlerm.pgn",
            "Grandmaster",
            "b. 1960",
            @"<p>Murray Chandler is a chess Grandmaster, author, and tournament organiser who represented New Zealand and England. He played for England in four Chess Olympiads, winning silver medals in 1984, 1986, and 1988.</p>
              <p>He was editor of <em>British Chess Magazine</em> (1991–1999) and founded Gambit Publications, one of the world's leading chess publishing houses.</p>"
        ),
        new(
            "Paul Littlewood",
            "paul-littlewood",
            "littlewoodpaul.pgn",
            "International Master",
            "b. 1956",
            @"<p>Paul Littlewood won the British Chess Championship in 1981 at Morecambe with a score of 9/11. He was awarded the International Master title in 1980.</p>
              <p>Coming from a famous English chess family (son of John Littlewood), he represented England at the 1982 Lucerne Olympiad.</p>"
        ),
        new(
            "Anthony J Miles",
            "anthony-j-miles",
            "milestony.pgn",
            "Grandmaster",
            "1955 – 2001",
            @"<p>Tony Miles made history in 1976 by becoming the first British-born over-the-board Grandmaster, winning a £5,000 prize offered by Jim Slater.</p>
              <p>Known for his imaginative style and unorthodox openings, he famously defeated World Champion Anatoly Karpov in 1980 playing 1.e4 a6!? (the St. George Defence).</p>"
        ),
        new(
            "Nigel Short",
            "nigel-short",
            "shortn.pgn",
            "Grandmaster",
            "b. 1965",
            @"<p>Nigel Short is one of the most prominent figures in British chess history. In 1993, he defeated Anatoly Karpov and Jan Timman to challenge Garry Kasparov for the World Chess Championship in London.</p>
              <p>A prodigy who qualified for the British Championship at age 12, he became a Grandmaster at 19 and reached a peak world ranking of No. 3.</p>"
        ),
        new(
            "Ian D Wells",
            "ian-d-wells",
            "wells_ian.pgn",
            "FIDE Master",
            "1964 – 1982",
            @"<p>Ian Wells was one of England's brightest young chess talents in the late 1970s and early 1980s. A contemporary and rival of Nigel Short, he earned the British Under-18 title and qualified as a FIDE Master.</p>
              <p>Tragically, he passed away at the age of 17 during a tournament in the Netherlands. His games remain a testament to his attacking brilliance.</p>"
        )
    ];

    public static PlayerEntry GetBySlug(string? slug) =>
        All.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)) ?? All[0];
}