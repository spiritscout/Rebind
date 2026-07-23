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
        var (doc, idToHref) = LoadOpf(opfPath);

        // OPF elements live in this namespace; needed for every element query below.
        XNamespace opf = "http://www.idpf.org/2007/opf";

        // The spine lists item ids in reading order; preserve that order.
        var spineIds = doc.Descendants(opf + "itemref")
            .Select(itemref => itemref.Attribute("idref")!.Value)
            .ToList();

        // converts spineIDs and id->href lookups into list of hrefs, shows reading order as filepaths
        // TODO handle missing spine ID for malformed files
        var readingOrder = new List<string>();
        foreach (var id in spineIds)
        {
            readingOrder.Add(ResolveHref(opfPath, idToHref[id]));
        }
        return readingOrder;
    }

    public Dictionary<string, string> GetNavTitles(string opfPath)
    {
        var (doc, idToHref) = LoadOpf(opfPath);

        XNamespace opf = "http://www.idpf.org/2007/opf";

        // The spine's toc attribute names the manifest id of the nav document.
        var spine = doc.Descendants(opf + "spine").First();
        var tocId = spine.Attribute("toc")!.Value;

        // turns id into full nav path
        var navPath = ResolveHref(opfPath, idToHref[tocId]);

        // load nav file
        var navEntry = _archive.GetEntry(navPath);
        if (navEntry is null)
            throw new InvalidOperationException($"Nav document not found in archive at path: {navPath}");

        XDocument navDoc;
        using (var stream = navEntry.Open())
        {
            navDoc = XDocument.Load(stream);
        }

        // NCX has its own namespace, distinct from the OPF's.
        XNamespace ncx = "http://www.daisy.org/z3986/2005/ncx/";

        // walk nav points and extract them at any depth
        var navTitles = new Dictionary<string, string>();

        foreach (var navPoint in navDoc.Descendants(ncx + "navPoint"))
        {
            // pull out title and src, 2 hops to descend from navpoint *then* read or extract, respectively
            var title = navPoint.Element(ncx + "navLabel")!.Element(ncx + "text")!.Value;
            var src = navPoint.Element(ncx + "content")!.Attribute("src")!.Value;

            // Nav src may carry an anchor (file.html#section); spine needs plain paths.
            var hashIndex = src.IndexOf('#');
            if (hashIndex >= 0)
                src = src[..hashIndex];

            // get full archive path of src and add it to navTitles
            var srcPath = ResolveHref(opfPath, src);
            if (!navTitles.ContainsKey(srcPath))
            {
                navTitles.Add(srcPath, title);
            }
        }

        return navTitles;
    }

    // Loads the OPF and builds the manifest id -> href lookup.
    // Both public methods need these, so they're built in one place.
    private (XDocument Doc, Dictionary<string, string> IdToHref) LoadOpf(string opfPath)
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

        return (doc, idToHref);
    }

    // Hrefs inside the OPF are relative to the OPF's own folder, not the
    // archive root. This resolves them to full archive paths.
    // TODO - "../" not handled
    private static string ResolveHref(string opfPath, string href)
    {
        var opfDirectory = Path.GetDirectoryName(opfPath);

        // OPF at the archive root: the href is already the full path.
        if (string.IsNullOrEmpty(opfDirectory))
            return href;

        // Zip paths always use forward slashes, so don't use Path.Combine here.
        return $"{opfDirectory.Replace('\\', '/')}/{href}";
    }
}