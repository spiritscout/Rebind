using System.IO.Compression;
using System.Xml.Linq;

namespace Rebind.Core.Reading;

public class EpubReader
{
    // The EPUB this specific reader is bound to, for the lifetime of the object.
    // Held once so methods don't each need it passed in.
    private readonly ZipArchive _archive;
    
    public EpubReader(ZipArchive archive)
    {
        _archive = archive;
    }
    public string FindOpfPath()
    {
        var containerEntry = _archive.GetEntry("META-INF/container.xml");
        if (containerEntry is null)
            throw new InvalidOperationException("Not a valid EPUB: META-INF/container.xml is missing");

        XDocument doc;
        using (var stream = containerEntry.Open())
        {
            doc = XDocument.Load(stream);
        }

        XNamespace ns = "urn:oasis:names:tc:opendocument:xmlns:container";

        var rootfile = doc.Descendants(ns + "rootfile").First();
        var pathAttribute = rootfile.Attribute("full-path");

        // TODO null check
        return pathAttribute.Value;
    }
}