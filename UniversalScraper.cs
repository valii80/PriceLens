using System.Net.Http;

namespace PriceLens;

public class UniversalScraper : IScraper
{
    private readonly HttpClient _httpClient;

    public UniversalScraper()
    {
        _httpClient = new HttpClient();

        // Browser simulieren (wichtig!)
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task<List<Angebot>> ScrapeAsync(string suchbegriff)
    {
        var angebote = new List<Angebot>();

        var urls = new List<string>
{
    $"https://www.ebay.de/sch/i.html?_nkw={suchbegriff}",
    $"https://www.kleinanzeigen.de/s-{suchbegriff.Replace(" ", "-")}/k0"
};

        foreach (var url in urls)
        {
            Console.WriteLine("=================================");
            Console.WriteLine($"Lade: {url}");

            try
            {
                string html = await _httpClient.GetStringAsync(url);

                Console.WriteLine("HTML geladen!");

                // kurzer Debug-Auszug
                Console.WriteLine(html.Substring(0, Math.Min(300, html.Length)));

                // einfache Preisprüfung
                if (html.Contains("EUR") || html.Contains("€"))
                {
                    Console.WriteLine("→ Preise gefunden 💥");
                }
                else
                {
                    Console.WriteLine("→ Keine Preise ❌");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler:");
                Console.WriteLine(ex.Message);
            }
        }

        return angebote;
    }
}