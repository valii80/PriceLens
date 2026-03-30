using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace PriceLens;

public class SearchService
{
    private readonly ScraperManager scraper;
    private readonly BewertungsService bewertung;

    public SearchService()
    {
        scraper = new ScraperManager();
        bewertung = new BewertungsService();
    }

    public async Task<List<Angebot>> Search(string query)
    {
        var daten = await scraper.LadeDaten(query);

        // Filter (optional)
        var filter = new FilterService();
        daten = filter.Filter(daten, query);

        // Duplikate entfernen
        daten = daten
            .GroupBy(x => x.produkt?.name ?? "")
            .Select(g => g.First())
            .ToList();

        return bewertung.Rank(daten, query);
    }

}