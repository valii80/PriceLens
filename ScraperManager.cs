using System.Collections.Generic;
using System.Threading.Tasks;

namespace PriceLens;

public class ScraperManager
{
    private List<IScraper<List<Angebot>>> scraper = new();
    public ScraperManager()
    {
        scraper.Add(new EbayApiScraper());
    }

    public async Task<List<Angebot>> LadeDaten(string suchbegriff)
    {
        var results = new List<Angebot>();

        foreach (var s in scraper)
        {
            var data = await s.ScrapeAsync(suchbegriff);
            results.AddRange(data);
        }

        return results;
    }
}