namespace Rebind.Core.Reading;

public class ContentFilter
{
    public List<SpineEntry> Filter(List<SpineEntry> entries)
    {
        throw new NotImplementedException();
    }

    // Dropped wherever they appear, not just at end: can crop up in front matter too, so position-dependence doesn't apply.
    private static readonly string[] AlwaysSkip =
    [
        "also by",
        "about the author",
    ];
}