namespace PriceLens;

public interface IScraper
{
    Task<List<Angebot>> ScrapeAsync(string suchbegriff);
}