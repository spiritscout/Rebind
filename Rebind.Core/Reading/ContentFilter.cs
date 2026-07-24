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
        "uncopyright",
        "colophon",
        "praise for",
        "preview of",
        "excerpt from",
    ];

    public List<SpineEntry> Filter(List<SpineEntry> entries)
    {
        var kept = new List<SpineEntry>();
        foreach (var entry in entries)
        {
            if (IsAlwaysSkipped(entry))
                continue;

            kept.Add(entry);
        }
        return kept;
    }

    private static bool IsAlwaysSkipped(SpineEntry entry)
    {
        // Untitled entries are kept
        if (entry.Title is null)
            return false;

        // return true if the title contains any AlwaysSkip keyword.
        foreach (var keyword in AlwaysSkip)
        {
            if (entry.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}