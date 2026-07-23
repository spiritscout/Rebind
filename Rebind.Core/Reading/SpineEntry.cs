namespace Rebind.Core.Reading;

// One entry in the spine: a content file and its nav title, if it has one.
// Title is nullable because some spine files have no nav entry at all.
public record SpineEntry(string Path, string? Title);