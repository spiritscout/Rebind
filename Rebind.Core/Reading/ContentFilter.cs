using System.Text.RegularExpressions;

namespace Rebind.Core.Reading;

public class ContentFilter
{

    // Dropped wherever they appear, not just at end: can crop up in front matter too, so position-dependence doesn't apply.
    private static readonly string[] AlwaysSkip =
    [
        "also by",
        "about the author",
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
        var frontRegionEnd = entries.Count;

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

}