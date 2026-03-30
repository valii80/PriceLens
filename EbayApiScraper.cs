using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PriceLens;

public class EbayApiScraper : IScraper
{
    private readonly EbayApiClient client = new();

    public async Task<List<Angebot>> ScrapeAsync(string suchbegriff)
    {
        var de = await client.SearchAsync(suchbegriff, "EBAY_DE");
        var us = await client.SearchAsync(suchbegriff, "EBAY_US");
        var uk = await client.SearchAsync(suchbegriff, "EBAY_GB");

        return de.Concat(us).Concat(uk).ToList();
    }

    public string GetSourceName()
    {
        return "eBay";
    }
}