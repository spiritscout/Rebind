using System.IO.Compression;

namespace Rebind.Core.Reading;

public class ChapterReader
{
    // Bound to one archive for the object's lifetime, same as EpubReader.
    private readonly ZipArchive _archive;

    public ChapterReader(ZipArchive archive)
    {
        _archive = archive;
    }

    public Chapter Read(SpineEntry entry)
    {
        // TODO: read entry.Path from the archive, parse the HTML,
        // turn every <p> into a Paragraph, assemble the Chapter.
        throw new NotImplementedException();
    }
}