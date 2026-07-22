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

    public List<string> GetReadingOrder(string opfPath)
    {
        var opfEntry = _archive.GetEntry(opfPath);
        if (opfEntry is null)
            throw new InvalidOperationException($"OPF not found in archive at path: {opfPath}");

        XDocument doc;
        using (var stream = opfEntry.Open())
        {
            doc = XDocument.Load(stream);
        }

        // OPF elements live in this namespace; needed for every element query below.
        XNamespace opf = "http://www.idpf.org/2007/opf";

        var manifestItems = doc.Descendants(opf + "item");

        // Map each manifest id to its file path (href), so the spine's
        // id references can be resolved to actual files. ToDict(key, value)
        var idToHref = manifestItems.ToDictionary(
            item => item.Attribute("id")!.Value,
            item => item.Attribute("href")!.Value
        );

        // The spine lists item ids in reading order; preserve that order.
        var spineIds = doc.Descendants(opf + "itemref")
            .Select(itemref => itemref.Attribute("idref")!.Value)
            .ToList();

        // converts spineIDs and id->href lookups into list of hrefs, shows reading order as filepaths
        // TODO handle missing spine ID for malformed files
        var readingOrder = new List<string>();
        foreach (var id in spineIds)
        {
            readingOrder.Add(idToHref[id]);
        }
        return readingOrder;
        
    }
}