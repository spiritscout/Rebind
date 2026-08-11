using System.IO.Compression;
using Rebind.Core.Reading;
using Rebind.Core;

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

// Pair each spine path with its nav title, if it has one
var entries = new List<SpineEntry>();
foreach (var filePath in readingOrder)
{
    var title = navTitles.TryGetValue(filePath, out var found) ? found : null;
    entries.Add(new SpineEntry(filePath, title));
}

var filter = new ContentFilter();
var filtered = filter.Filter(entries);

var (kept, dropped) = filter.Filter(entries);

Console.WriteLine($"\nReading order ({kept.Count} of {entries.Count} entries kept):");
foreach (var entry in kept)
{
    var display = entry.Title ?? "(no title)";
    Console.WriteLine($"  {display,-40} {entry.Path}");
}

if (dropped.Count > 0)
{
    Console.WriteLine("\nDropped:");
    foreach (var entry in dropped)
    {
        Console.WriteLine($"  {entry.Title ?? "(no title)"}");
    }
}

// Slice one test - read first kept chapter, see what comes out
var chapterReader = new ChapterReader(archive);
var firstChapter = chapterReader.Read(kept[1]);

Console.WriteLine($"\nFirst chapter: {firstChapter.Title ?? "(no title)"}");
Console.WriteLine($"Blocks: {firstChapter.Content.Count}");

// Show block types so recogniser output is visible, not just paragraphs.
foreach (var block in firstChapter.Content.Take(4))
{
    switch (block)
    {
        case BookImage image:
            Console.WriteLine($"\n  [BookImage] src={image.Source} alt={image.Alt ?? "(none)"}");
            break;
        case Paragraph paragraph:
            Console.WriteLine($"\n  [Paragraph] {paragraph.Html}");
            break;
        default:
            Console.WriteLine($"\n  [{block.GetType().Name}]");
            break;
    }
}

