using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PriceLens;

public class EbayApiClient : IScraper<List<Angebot>>
{
    private readonly HttpClient httpClient = new();

    // ⚠️ API KEY EBAY (vor GitHub Push entfernen!)
    private readonly string clientId = "";
    private readonly string clientSecret = "";


    // TOKEN HOLEN
    public async Task<List<Angebot>> SearchAsync(string query, string marketplace)
    {
        var tokenJson = await GetAccessTokenAsync();

        var token = System.Text.Json.JsonDocument
            .Parse(tokenJson)
            .RootElement
            .GetProperty("access_token")
            .GetString();

        httpClient.DefaultRequestHeaders.Clear();

        // 🔑 Token setzen
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        //  Region setzen
        httpClient.DefaultRequestHeaders.Add("X-EBAY-C-MARKETPLACE-ID", marketplace);

        // 📍 Kontext (Deutschland)
        httpClient.DefaultRequestHeaders.Add("X-EBAY-C-ENDUSERCTX", "contextualLocation=country=DE");

        // Query normalisieren
        query = query.Replace(",", ".");

        var encodedQuery = Uri.EscapeDataString(query);

        // ===============================
        // PAGINATION START
        // ===============================
        var result = new List<Angebot>();

        int limit = 50;     // max pro Anfrage
        int maxPages = 5;   // wie viele Seiten du laden willst

        for (int page = 0; page < maxPages; page++)
        {
            int offset = page * limit;

            // URL mit OFFSET
            var url = $"https://api.ebay.com/buy/browse/v1/item_summary/search?q={encodedQuery}&limit=50";

            // 📡 Anfrage senden
            var response = await httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            var doc = System.Text.Json.JsonDocument.Parse(json);

            // ❌ Keine Daten → abbrechen
            if (!doc.RootElement.TryGetProperty("itemSummaries", out var items))
                break;

            int count = 0;

            // ===============================
            // PARSING
            // ===============================
            foreach (var item in items.EnumerateArray())
            {
                count++;

                var angebot = new Angebot();

                // Produktdaten
                angebot.produkt = new Produkt
                {
                    name = item.GetProperty("title").GetString() ?? "",
                    kategorie = "eBay",
                    bewertung = 0
                };

                // Preis
                if (item.TryGetProperty("price", out var priceProp))
                {
                    angebot.preis = decimal.Parse(
                        priceProp.GetProperty("value").GetString() ?? "0",
                        System.Globalization.CultureInfo.InvariantCulture
                    );

                    angebot.waehrung = priceProp.GetProperty("currency").GetString() ?? "";
                }

                // Shop
                angebot.shop = new Shop
                {
                    name = marketplace,
                    url = marketplace switch
                    {
                        "EBAY_DE" => "https://www.ebay.de",
                        "EBAY_US" => "https://www.ebay.com",
                        "EBAY_GB" => "https://www.ebay.co.uk",
                        _ => "https://www.ebay.com"
                    }
                };

                result.Add(angebot);
            }

            // ❌ Wenn Seite leer → keine weiteren Seiten vorhanden
            if (count == 0)
                break;
        }

        // ===============================
        // ALLE SEITEN ZURÜCKGEBEN
        // ===============================
        return result;
    }

    // Get Access Token from eBay API
    private async Task<string> GetAccessTokenAsync()
    {
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")
        );

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        var content = new StringContent(
            "grant_type=client_credentials&scope=https://api.ebay.com/oauth/api_scope",
            Encoding.UTF8,
            "application/x-www-form-urlencoded"
        );

        var response = await httpClient.PostAsync(
            "https://api.ebay.com/identity/v1/oauth2/token",
            content
        );

        return await response.Content.ReadAsStringAsync();
    }
        public async Task<List<Angebot>> ScrapeAsync(string suchbegriff)
    {
        var result = new List<Angebot>();

        result.AddRange(await SearchAsync(suchbegriff, "EBAY_DE"));
        result.AddRange(await SearchAsync(suchbegriff, "EBAY_US"));
        result.AddRange(await SearchAsync(suchbegriff, "EBAY_GB"));

        return result;
    }
}
