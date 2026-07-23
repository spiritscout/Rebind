/* 
using System.IO.Compression;

if (args.Length == 0)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  <epub>                          list entries");
    Console.WriteLine("  <epub> <file-substr>            dump start of file");
    Console.WriteLine("  <epub> <file-substr> <chars>    dump N chars");
    Console.WriteLine("  <epub> <file-substr> find:<term>  window around first match");
    return;
}

string path = args[0];
string? filter = args.Length > 1 ? args[1] : null;
using ZipArchive zip = ZipFile.OpenRead(path);

// Global search: find which content file contains a phrase.
// Usage: <epub> all "search phrase"
if (filter == "all" && args.Length > 2)
{
    string needle = args[2];
    foreach (var e in zip.Entries.Where(e =>
        e.FullName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
        e.FullName.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase) ||
        e.FullName.EndsWith(".htm", StringComparison.OrdinalIgnoreCase)))
    {
        using var r = new StreamReader(e.Open());
        if (r.ReadToEnd().Contains(needle, StringComparison.OrdinalIgnoreCase))
            Console.WriteLine($"MATCH: {e.FullName}");
    }
    return;
}

if (filter is null)
{
    foreach (var entry in zip.Entries)
        Console.WriteLine($"{entry.Length,10}  {entry.FullName}");
    return;
}

var target = zip.Entries.FirstOrDefault(e =>
    e.FullName.Contains(filter, StringComparison.OrdinalIgnoreCase));
if (target is null) { Console.WriteLine($"No entry matched \"{filter}\"."); return; }

using var reader = new StreamReader(target.Open());
string text = reader.ReadToEnd();
string? third = args.Length > 2 ? args[2] : null;

if (third is not null && third.StartsWith("find:", StringComparison.OrdinalIgnoreCase))
{
    string term = third["find:".Length..];
    int idx = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
    if (idx < 0) { Console.WriteLine($"\"{term}\" not found."); return; }
    int start = Math.Max(0, idx - 400);
    int end = Math.Min(text.Length, idx + 1600);
    Console.WriteLine($"=== {target.FullName} — window around \"{term}\" ===");
    Console.WriteLine(text[start..end]);
}
else
{
    int maxChars = third is not null && int.TryParse(third, out int m) ? m : 4000;
    Console.WriteLine($"=== {target.FullName} (up to {maxChars} chars) ===");
    Console.WriteLine(text[..Math.Min(maxChars, text.Length)]);
}

*/

using System.IO.Compression;
using Rebind.Core.Reading;

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run --project Rebind.Cli -- <path-to-epub>");
    return;
}

string path = args[0];

using ZipArchive archive = ZipFile.OpenRead(path);


var reader = new EpubReader(archive);
var epubOpfPath = reader.FindOpfPath();
Console.WriteLine(epubOpfPath);

var readingOrder = reader.GetReadingOrder(epubOpfPath);

var navTitles = reader.GetNavTitles(epubOpfPath);

Console.WriteLine("\nReading order:");
foreach (var filePath in readingOrder)
{
    var title = navTitles.TryGetValue(filePath, out var found) ? found : "(no title)";
    Console.WriteLine($"  {title,-40} {filePath}");
}
