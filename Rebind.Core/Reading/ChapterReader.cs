using System.IO.Compression;
using AngleSharp.Html.Parser;

namespace Rebind.Core.Reading;

public class ChapterReader
{
    private readonly ZipArchive _archive;

    public ChapterReader(ZipArchive archive)
    {
        _archive = archive;
    }

    public Chapter Read(SpineEntry entry)
    {
        // Locate the content file. A null here is the missing-spine-target case
        // the spec flagged (Jekyll chapter-4): a clear error, not a silent crash.
        var contentEntry = _archive.GetEntry(entry.Path);
        if (contentEntry is null)
            throw new InvalidOperationException($"Content file not found in archive at path: {entry.Path}");

        // Read the markup as a string. StreamReader defaults to UTF-8, which
        // every specimen so far is; a latent assumption if an exotic one appears.
        string html;
        using (var stream = contentEntry.Open())
        using (var streamReader = new StreamReader(stream))
        {
            html = streamReader.ReadToEnd();
        }

        // First AngleSharp contact: parse the markup into a queryable DOM.
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // TODO: query every <p>, wrap each as a Paragraph, assemble the Chapter.
        throw new NotImplementedException();
    }
}