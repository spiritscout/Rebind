using System.IO.Compression;
using AngleSharp.Html.Parser;
using AngleSharp.Dom;

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

        // Slice one: every <p> becomes Paragraph, in document order. No
        // filtering or cleanup yet. InnerHtml, not OuterHtml: the Paragraph type
        // already means "a paragraph", so we store only its contents and let the
        // renderer supply the <p> wrapper later. The source's own <p class="...">
        // cruft is dropped for free as a result
        var blocks = new List<Block>();
        foreach (var p in document.QuerySelectorAll("p"))
        {
            var imageBlocks = RecogniseImage(p);
            if (imageBlocks is not null)
            {
                blocks.AddRange(imageBlocks);
                continue;
            }

            blocks.Add(new Paragraph(p.InnerHtml));
        }

        // Nav title used directly for slice one. In-file-heading-vs-nav-title
        // preference is deferred to the Heading recogniser, which can't run until
        // headings are being parsed anyway
        return new Chapter(entry.Title, blocks);
    }

    // Recognises an image-only paragraph: one or more <img> and no prose text.
    // Returns one BookImage per image, or null if this <p> is not image-only,
    // in which case the caller treats it as an ordinary Paragraph.
    private static List<Block>? RecogniseImage(IElement p)
    {
        var images = p.QuerySelectorAll("img");
        if (images.Length == 0 || !string.IsNullOrWhiteSpace(p.TextContent))
            return null;

        var blocks = new List<Block>();
        foreach (var img in images)
        {
            var src = img.GetAttribute("src") ?? "";
            var alt = img.GetAttribute("alt");
            blocks.Add(new BookImage(src, alt));
        }
        return blocks;
    }
}