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

    public async Task<(List<Angebot> raw, List<Angebot> gefiltert, List<Angebot> cleaned, List<Angebot> ranked)> Search(string query)
    {
        // ROHE ERGEBNISSE
        var raw = await scraper.LadeDaten(query);

        // FILTER
        var filter = new FilterService();
        var gefiltert = filter.Filter(raw, query);

        // DUPPLIKATE ENTFERNT
        var cleanedResults = gefiltert
            .GroupBy(a => a.produkt?.name ?? "")
            .Select(g => g.First())
            .ToList();

        // RANK (ÜBRIG ZUR VERFÜGUNG)
        var ranked = bewertung.Rank(cleanedResults, query);

        return (raw, gefiltert, cleanedResults, ranked);
    }
}