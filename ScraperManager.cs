namespace PriceLens;

public class ScraperManager
{
    private List<IScraper> scraperList = new();

    public void AddScraper(IScraper scraper)
    {
        scraperList.Add(scraper);
    }

    public async Task<List<Angebot>> LadeDaten(string suchbegriff)
    {
        var alleAngebote = new List<Angebot>();

        foreach (var scraper in scraperList)
        {
            var angebote = await scraper.ScrapeAsync(suchbegriff);
            alleAngebote.AddRange(angebote);
        }

        return alleAngebote;
    }
}