using System.Text.RegularExpressions;

namespace Rebind.Core.Reading;

public class ContentFilter
{

    // Dropped wherever they appear, not just at end: can crop up in front matter too, so position-dependence doesn't apply.
    private static readonly string[] AlwaysSkip =
    [
        "also by",
        "about the author",
        "colophon",
        "uncopyright",
    ];

    // Front matter only. Applied within the pre-block window, or from the
    // start of the list when no chapter block is detected.
    private static readonly string[] FrontMatterSkip =
    [
        "cover",
        "title page",
        "titlepage",
        "copyright",
        "contents",
        "imprint",
        "uncopyright",  // not covered by "copyright" once word boundaries apply
        "colophon",
        "praise for",
        "preview of",
        "excerpt from",
    ];

    public (List<SpineEntry> Kept, List<SpineEntry> Dropped) Filter(List<SpineEntry> entries)
    {
        var kept = new List<SpineEntry>();
        var dropped = new List<SpineEntry>();

        // Everything before this index is a front-matter candidate.
        // With no block detection, the whole list is candidate
        var frontRegionEnd = FindBlockStart(entries) ?? entries.Count;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            if (IsAlwaysSkipped(entry))
            {
                dropped.Add(entry);
                continue;
            }

            if (i < frontRegionEnd && IsFrontMatter(entry))
            {
                dropped.Add(entry);
                continue;
            }

            kept.Add(entry);
        }
        return (kept, dropped);
    }

    private static bool IsAlwaysSkipped(SpineEntry entry) => MatchesAny(entry, AlwaysSkip);

    private static bool IsFrontMatter(SpineEntry entry) => MatchesAny(entry, FrontMatterSkip);

    // Word-boundary matching, so "cover" doesn't match "discover" or "recovery".
    // Strictly more conservative than Contains: it can only reduce what matches,
    // never widen it, so it cannot cause a wanted entry to be dropped.
    private static bool MatchesAny(SpineEntry entry, string[] keywords)
    {
        // Untitled entries are kept: we don't know what they are.
        if (entry.Title is null)
            return false;

        foreach (var keyword in keywords)
        {
            // \b is a word boundary. Regex.Escape guards against a keyword
            // later containing regex-special characters.
            var pattern = $@"\b{Regex.Escape(keyword)}\b";
            if (Regex.IsMatch(entry.Title, pattern, RegexOptions.IgnoreCase))
                return true;
        }

        return false;
    }

    // All Arabic numbers in the string, in order. "c01_r1" -> [1, 1].
    // long, not int - filenames can embed a 13-digit ISBN that overflows int.
    private static List<long> ExtractNumbers(string text)
    {
        var numbers = new List<long>();
        foreach (Match match in Regex.Matches(text, @"\d+"))
        {
            numbers.Add(long.Parse(match.Value));
        }
        return numbers;
    }

    // Longest stretch of values each exactly one more than the last.
    // Returns the run's length and where it starts.
    private static (int RunLength, int StartIndex) LongestConsecutiveRun(List<long> numbers)
    {
        if (numbers.Count == 0)
            return (0, 0);

        int bestLength = 1, bestStart = 0;
        int currentLength = 1, currentStart = 0;

        for (int i = 1; i < numbers.Count; i++)
        {
            if (numbers[i] == numbers[i - 1] + 1)
            {
                currentLength++;
            }
            else
            {
                currentLength = 1;
                currentStart = i;
            }

            if (currentLength > bestLength)
            {
                bestLength = currentLength;
                bestStart = currentStart;
            }
        }

        return (bestLength, bestStart);
    }

    // The chapter block: longest consecutive numeric run, from titles first,
    // then filenames. Returns the entry index where it starts, or null.
    // Titles before filenames is load-bearing: ASOS/BUTTERFLY filenames would
    // yield a spurious full-length run, avoided only because titles match first.
    private int? FindBlockStart(List<SpineEntry> entries)
    {
        var fromTitles = DetectBlock(entries, e => e.Title ?? "");
        if (fromTitles is not null)
            return fromTitles;

        return DetectBlock(entries, e => e.Path);
    }

    // Runs block detection over one text source (title or path).
    private int? DetectBlock(List<SpineEntry> entries, Func<SpineEntry, string> textOf)
    {
        // Numbers per entry, from the chosen source.
        var perEntry = entries.Select(e => ExtractNumbers(textOf(e))).ToList();

        // Widest row tells us how many columns (number positions) to test.
        int maxColumns = perEntry.Count == 0 ? 0 : perEntry.Max(nums => nums.Count);

        int? bestStart = null;
        int bestLength = 0;

        for (int col = 0; col < maxColumns; col++)
        {
            // Gather this column: (real entry index, the number at this position).
            var entryIndices = new List<int>();
            var columnNumbers = new List<long>();
            for (int i = 0; i < perEntry.Count; i++)
            {
                if (col < perEntry[i].Count)
                {
                    entryIndices.Add(i);
                    columnNumbers.Add(perEntry[i][col]);
                }
            }

            var (runLength, runStart) = LongestConsecutiveRun(columnNumbers);

            //  TODO inline 5 declared, may require named const later
            if (runLength >= 5 && runLength > bestLength)
            {
                bestLength = runLength;
                // Map the run's position back to the real entry index.
                bestStart = entryIndices[runStart];
            }
        }

        return bestStart;
    }

}