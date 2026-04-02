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

                int maxPages = 2;
                for (int pageIndex = 1; pageIndex <= maxPages; pageIndex++)
                {
                    if (page == null || page.IsClosed) break;
                    string pagedUrl = pageIndex == 1 ? url : $"{url}&pg={pageIndex}";

                    await page.GotoAsync(pagedUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                    await page.WaitForTimeoutAsync(2000);
                    
                    //===================================
                    // COOKIES automatisch akzeptieren
                    //===================================
                    if (pageIndex == 1)
                    {
                        string[] cookieTexts = { "Alle akzeptieren", "Accept all", "Akzeptieren", "Zustimmen" };
                        bool cookieClicked = false;
                        foreach (var text in cookieTexts)
                        {
                            try
                            {
                                await page.ClickAsync($"button:has-text('{text}')", new() { Timeout = 2000 });
                                Console.WriteLine($"✅ Cookies: {text}");
                                cookieClicked = true; break;
                            }
                            catch { }
                        }
                        if (!cookieClicked) finalResult.Logs.Add("⚠️ Kein Cookie Popup gefunden");
                        
                        //===================
                        // OVERLAY KILLER
                        //===================
                        try
                        {
                            await page.EvaluateAsync("document.querySelectorAll('*').forEach(el => { if (window.getComputedStyle(el).position === 'fixed') el.remove(); });");
                            Console.WriteLine("✅ Overlay entfernt");
                        }
                        catch { }
                    }

                    //======================
                    // SCROLL & SEARCH
                    //======================
                    for (int i = 0; i < 2; i++) { await page.EvaluateAsync("window.scrollBy(0, window.innerHeight)"); await page.WaitForTimeoutAsync(500); }

                    var elements = await page.QuerySelectorAllAsync("article.galleryview__item");
                    totalCount += elements.Count;

                    foreach (var el in elements)
                    {
                        try
                        {
                            var titleEl = await el.QuerySelectorAsync("a.galleryview__name-link");
                            var priceEl = await el.QuerySelectorAsync("span.gh_price");
                            var title = titleEl != null ? (await titleEl.InnerTextAsync()).Trim() : "";
                            var priceText = priceEl != null ? await priceEl.InnerTextAsync() : "";

                            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(priceText)) { filteredOut++; continue; }

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
            catch (Exception ex) { finalResult.Logs.Add($"❌ Fehler: {ex.Message}"); return finalResult; }
        }

        private double ParsePrice(string text)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+[.,]\d+");
            return match.Success ? double.Parse(match.Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture) : 0;
        }
    }

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