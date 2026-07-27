// Exploration script - temp


using System.IO.Compression;

string path = args[0];
using ZipArchive zip = ZipFile.OpenRead(path);

if (args.Length < 2)
{
    // No filter: list content files so we can pick one.
    foreach (var entry in zip.Entries.Where(e =>
        e.FullName.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase) ||
        e.FullName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)))
    {
        Console.WriteLine($"{entry.Length,8}  {entry.FullName}");
    }
    return;
}

// Filter given: dump the first matching file whole.
var target = zip.Entries.First(e =>
    e.FullName.Contains(args[1], StringComparison.OrdinalIgnoreCase));

using var reader = new StreamReader(target.Open());
Console.WriteLine(reader.ReadToEnd());