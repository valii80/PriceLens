using PriceLens;
using System.Net.Http.Headers;
using System.Text.Json;

public class EbayApiScraper : IScraper
{
    private readonly HttpClient _http = new HttpClient();
    private readonly string _token;

    public EbayApiScraper(string token)
    {
        _token = token;
    }

    public async Task<List<Angebot>> ScrapeAsync(string suchbegriff)
    {
        var angebote = new List<Angebot>();

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);

        var url = $"https://api.ebay.com/buy/browse/v1/item_summary/search?q={suchbegriff}";

        var json = await _http.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);

        var items = doc.RootElement.GetProperty("itemSummaries");

        foreach (var item in items.EnumerateArray())
        {
            try
            {
                var title = item.GetProperty("title").GetString() ?? "Unbekannt";

                if (!item.TryGetProperty("price", out var priceObj))
                    continue;

                if (!priceObj.TryGetProperty("value", out var valueProp))
                    continue;

                var priceString = valueProp.GetString() ?? "";

                if (!decimal.TryParse(priceString, out var price))
                    continue;

                angebote.Add(new Angebot
                {
                    preis = price,
                    waehrung = "EUR",
                    produkt = new Produkt { name = title },
                    shop = new Shop { name = "eBay", url = "ebay.de" }
                });
            }
            catch
            {
                // API ist nicht immer sauber → ignorieren
            }
        }

        return angebote;
    }
}