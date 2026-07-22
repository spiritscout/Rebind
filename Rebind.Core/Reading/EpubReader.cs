using System.IO.Compression;
using System.Xml.Linq;

namespace Rebind.Core.Reading;

public class EpubReader
{
    public string FindOpfPath(ZipArchive archive)
    {
        var containerEntry = archive.GetEntry("META-INF/container.xml");
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