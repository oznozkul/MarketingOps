using System.Xml;
using System.Xml.Linq;

namespace MarketingOps.Infrastructure.Crawling;

public static class SitemapParser
{
    public static SitemapContent Parse(string xml)
    {
        XDocument doc;
        try
        {
            // UTF-8 BOM gövdeyi çözen GetString'den sonra metnin başında kalır; XML bildirimi ilk karakter olmalı.
            doc = XDocument.Parse(xml.TrimStart('﻿', ' ', '\t', '\r', '\n'));
        }
        catch (XmlException)
        {
            return new SitemapContent([], []);
        }

        // Namespace'e bakmadan "loc" öğeleri okunur; bazı siteler standart dışı namespace kullanıyor.
        var root = doc.Root;
        if (root is null) return new SitemapContent([], []);
        var locs = root.Elements()
            .Select(e => e.Elements().FirstOrDefault(c => c.Name.LocalName == "loc")?.Value.Trim())
            .Where(v => !string.IsNullOrEmpty(v))
            .Select(v => v!)
            .ToList();

        return root.Name.LocalName == "sitemapindex"
            ? new SitemapContent([], locs)
            : new SitemapContent(locs, []);
    }
}
