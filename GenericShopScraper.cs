using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PriceLens
{
    public class GenericShopScraper : IScraper<Produkt>
    {
        private readonly HttpClient _httpClient;
        public GenericShopScraper(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<Produkt> ScrapeAsync(string url) => new Produkt { name = "Nicht verwendet" };
        public async Task<ScrapeResult> ScrapeListAsync(string url)
        {
            var finalResult = new ScrapeResult();
            var results = new List<Produkt>();

            try
            {
                int totalCount = 0;
                int filteredOut = 0;

                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
                var page = await browser.NewPageAsync();

                await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> {
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36" }
                });

                //=============
                // PAGINATION
                //=============
                int maxPages = 3; // Begrenzung auf 3 Seiten für Demo-Zwecke (Anpassbar je nach Shop oder Wünsche)
                for (int pageIndex = 1; pageIndex <= maxPages; pageIndex++)
                {
                    if (page == null || page.IsClosed) break;
                    string pagedUrl = pageIndex == 1 ? url : $"{url}&pg={pageIndex}";

                    await page.GotoAsync(pagedUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                    await page.WaitForTimeoutAsync(2000);

                    //=================================
                    // COOKIES Europaweit akzeptieren
                    //=================================
                    if (pageIndex == 1)
                    {
                        // Sprachübergreifende Wortliste (DE, EN, FR, IT, ES, PL, NL, etc.)
                        string[] cookieTexts = {
                        "Akzeptieren", "Alle akzeptieren",
                        "Zustimmen", "OK", "Accept", "Accept all",
                        "Agree", "Allow all", "Accepter", "Tout accepter",
                        "Accetta", "Aceptar", "Aceptar todo", "Aceitar",
                        "Akceptuj", "Zgadzam się", "Souhlasím", "Súhlasím",
                        "Accepteren", "Elfogadom"};

                        bool cookieClicked = false;

                        // Erst nach Texten suchen (Timeout auf 500ms reduziert)
                        foreach (var text in cookieTexts)
                        {
                            try
                            {
                                await page.ClickAsync($"button:has-text('{text}')", new() { Timeout = 500 });
                                finalResult.Logs.Add($"🍪 Cookies:  erfolgreich bestätigt");
                                cookieClicked = true;
                                break;
                            }
                            catch { }
                        }

                        // FALLBACK: Technische Selektoren (Falls der Text ein Icon ist oder fehlt)
                        if (!cookieClicked)
                        {
                            string[] universalSelectors = {
                        "#onetrust-accept-btn-handler", // OneTrust
                        "#didomi-notice-agree-button",  // Didomi (Geizhals)
                        "button[id*='accept']",
                        "button[class*='accept']",
                        ".save-all",
                        "#accept-all"};

                            foreach (var sel in universalSelectors)
                            {
                                try
                                {
                                    await page.ClickAsync(sel, new() { Timeout = 500 });
                                    Console.WriteLine($"✅ Cookies (Technisch): {sel}");
                                    cookieClicked = true;
                                    break;
                                }
                                catch { }
                            }
                        }

                        if (!cookieClicked) finalResult.Logs.Add("⚠️ Kein Cookie-Popup bestätigt (eventuell bereits weg oder unbekannt)");

                        //====================================
                        // OVERLAY KILLER (wie POP-UP Promo)
                        //====================================
                        try
                        {
                            await page.EvaluateAsync("document.querySelectorAll('*').forEach(el => { if (window.getComputedStyle(el).position === 'fixed' || window.getComputedStyle(el).position === 'absolute') { if(el.innerText.length < 500) el.remove(); } });");
                            finalResult.Logs.Add("✅ Overlays: erfolgreich entfernt");
                        }
                        catch { }
                    }

                    //==================
                    // SCROLL & SEARCH
                    //==================
                    for (int i = 0; i < 2; i++) { await page.EvaluateAsync("window.scrollBy(0, window.innerHeight)"); await page.WaitForTimeoutAsync(500); }

                    var elements = await page.QuerySelectorAllAsync("article.galleryview__item, .productlist__item, .gl__item"); totalCount += elements.Count;

                    foreach (var el in elements)
                    {
                        try
                        {
                            var titleEl = await el.QuerySelectorAsync("a.galleryview__name-link, a.productlist__link, .gl__name");
                            var priceEl = await el.QuerySelectorAsync("span.gh_price, .gl__price, [class*='price']");
                            var title = titleEl != null ? (await titleEl.InnerTextAsync()).Trim() : "";
                            var priceText = priceEl != null ? await priceEl.InnerTextAsync() : "";

                            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(priceText) ||
                                priceText.Contains("Anfrage", StringComparison.OrdinalIgnoreCase))
                            {
                                filteredOut++;
                                continue;
                            }

                            double price = ParsePrice(priceText);
                            if (price <= 0) { filteredOut++; continue; }

                            results.Add(new Produkt { name = title, kategorie = "Geizhals", bewertung = price });

                        }
                        catch { filteredOut++; }
                    }
                }
                await browser.CloseAsync();

                finalResult.Produkte = results;
                finalResult.TotalGefunden = totalCount;
                finalResult.Gefiltert = filteredOut;
                return finalResult;
            }
            catch
            { finalResult.Logs.Add($"❌ Verbindung zu Geizhals fehlgeschlagen - bitte versuchen Sie erneut"); 
                return finalResult; 
            }
        }
        // ================================
        // PRICE PARSING (Text -> Double)
        // ================================
        private double ParsePrice(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0; var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+([.,]\d+)?");
            if (match.Success)
            {
                try
                {
                    string val = match.Value.Replace(",", ".");
                    return double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                }
                catch { return 0; }
            }
            return 0;
        }
    }

    //==================================
    //REGION DATEN-MODELLE & CONTAINER
    //==================================
    public class DisplayItem 
    { 
        public string? Name { get; set; } 
        public double Preis { get; set; } 
        public string? Shop { get; set; } 
        public object? OriginalObject { get; set; } }

    public class ScrapeResult
    {
        public List<Produkt> Produkte { get; set; } = new();
        public List<string> Logs { get; set; } = new();
        public int TotalGefunden { get; set; }
        public int Gefiltert { get; set; }
    }
}