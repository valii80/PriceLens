using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PriceLens;

public class EbayApiClient
{
    private readonly HttpClient httpClient = new();

    private readonly string clientId = "";
    private readonly string clientSecret = "";

    // 🔐 TOKEN HOLEN
    public async Task<List<Angebot>> SearchAsync(string query, string marketplace)
    {
        var tokenJson = await GetAccessTokenAsync();

        var token = System.Text.Json.JsonDocument
            .Parse(tokenJson)
            .RootElement
            .GetProperty("access_token")
            .GetString();

        httpClient.DefaultRequestHeaders.Clear();

        // 🔐 Token setzen
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // 🌍 REGION setzen (DAS IST DER KEY)
        httpClient.DefaultRequestHeaders.Add("X-EBAY-C-MARKETPLACE-ID", marketplace);

        // OPTIONAL aber stabil
        httpClient.DefaultRequestHeaders.Add("X-EBAY-C-ENDUSERCTX", "contextualLocation=country=DE");

        // 🔧 QUERY normalisieren
        query = query.Replace(",", ".");

        // Construct the URL with the filter for EUR currency
        var encodedQuery = Uri.EscapeDataString(query);

        // 🌐 URL
        var url = $"https://api.ebay.com/buy/browse/v1/item_summary/search?q={encodedQuery}&limit=10&filter=deliveryCountry:DE";
        
        // Send GET request
        var response = await httpClient.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();

        var result = new List<Angebot>();

        // Parse the response JSON
        var doc = System.Text.Json.JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("itemSummaries", out var items))
        {
            return new List<Angebot>();
        }

        // Process each item in the response
        foreach (var item in items.EnumerateArray())
        {
            var angebot = new Angebot();

            // Set product data
            angebot.produkt = new Produkt
            {
                name = item.GetProperty("title").GetString() ?? "",
                kategorie = "eBay",
                bewertung = 0
            };

            // Set price and currency
            if (item.TryGetProperty("price", out var priceProp))
            {
                angebot.preis = decimal.Parse(
                    priceProp.GetProperty("value").GetString() ?? "0",
                    System.Globalization.CultureInfo.InvariantCulture
                );

                angebot.waehrung = priceProp.GetProperty("currency").GetString() ?? "";
            }

            // Set shop data
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

            // Add to results
            result.Add(angebot);
        }

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
}