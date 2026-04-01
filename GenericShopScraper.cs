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

        public GenericShopScraper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Produkt> ScrapeAsync(string url)
        {
            return new Produkt
            {
                name = "Nicht verwendet",
                kategorie = "Web",
                bewertung = 0
            };
        }

        public async Task<List<Produkt>> ScrapeListAsync(string url)
        {
            var results = new List<Produkt>();

            try
            {
                int totalCount = 0;
                int index = 1;
                int filteredOut = 0;

                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new()
                {
                    Headless = false
                });

                var page = await browser.NewPageAsync();

                await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
                {
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36" }
                });

                // ===============================
                // PAGINATION
                // ===============================
                int maxPages = 2;

                for (int pageIndex = 1; pageIndex <= maxPages; pageIndex++)
                {
                    if (page == null || page.IsClosed)
                    {
                        Console.WriteLine("⚠️ Seite ist geschlossen → Abbruch");
                        break;
                    }

                    string pagedUrl = pageIndex == 1
                        ? url
                        : $"{url}&pg={pageIndex}";

                    Console.WriteLine("\n");

                    try
                    {
                        await page.GotoAsync(pagedUrl, new()
                        {
                            WaitUntil = WaitUntilState.DOMContentLoaded
                        });
                    }
                    catch
                    {
                        Console.WriteLine("⚠️ Navigation fehlgeschlagen → nächste Seite");
                        continue;
                    }

                    // WAIT
                    if (page != null && !page.IsClosed)
                        await page.WaitForTimeoutAsync(3000);

                    // ===============================
                    // COOKIES
                    // ===============================
                    if (pageIndex == 1)
                    {
                        string[] cookieTexts =
                        {
                            "Alle akzeptieren","Accept all","Akzeptieren","Accept",
                            "Zustimmen","Annehmen","Erlauben","Agree all","Agree"
                        };

                        bool cookieClicked = false;

                        foreach (var text in cookieTexts)
                        {
                            try
                            {
                                if (page != null && !page.IsClosed)
                                {
                                    await page.ClickAsync($"button:has-text('{text}')", new() { Timeout = 2000 });
                                    Console.WriteLine($"✅ Cookies: {text}");
                                    cookieClicked = true;
                                    break;
                                }
                            }
                            catch { }
                        }

                        if (!cookieClicked && page != null && !page.IsClosed)
                        {
                            foreach (var frame in page.Frames)
                            {
                                foreach (var text in cookieTexts)
                                {
                                    try
                                    {
                                        await frame.ClickAsync($"button:has-text('{text}')", new() { Timeout = 2000 });
                                        Console.WriteLine($"✅ Cookie iframe: {text}");
                                        cookieClicked = true;
                                        break;
                                    }
                                    catch { }
                                }
                                if (cookieClicked) break;
                            }
                        }

                        if (!cookieClicked)
                            Console.WriteLine("⚠️ Kein Cookie Popup gefunden");

                        try
                        {
                            if (page != null && !page.IsClosed)
                                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                        }
                        catch { }

                        if (page != null && !page.IsClosed)
                            await page.WaitForTimeoutAsync(3000);
                    }

                    // ===============================
                    // POPUP KILLER
                    // ===============================
                    try
                    {
                        if (page != null && !page.IsClosed)
                        {
                            await page.EvaluateAsync(@"
                                document.querySelectorAll('*').forEach(el => {
                                    const style = window.getComputedStyle(el);
                                    if ((style.position === 'fixed' || style.position === 'absolute') && el.innerText.length < 500) {
                                        el.remove();
                                    }
                                });
                            ");
                        }

                        if (pageIndex == 1)
                            Console.WriteLine("✅ Overlay entfernt");
                    }
                    catch
                    {
                        if (pageIndex == 1)
                            Console.WriteLine("⚠️ Overlay nicht gefunden");
                    }

                    if (page != null && !page.IsClosed)
                        await page.WaitForTimeoutAsync(2000);

                    // ===============================
                    // SCROLL
                    // ===============================
                    for (int i = 0; i < 3; i++)
                    {
                        if (page != null && !page.IsClosed)
                        {
                            await page.EvaluateAsync("window.scrollBy(0, window.innerHeight)");
                            await page.WaitForTimeoutAsync(1000);
                        }
                    }

                    // ===============================
                    // SCRAPING
                    // ===============================
                    if (page == null || page.IsClosed)
                    {
                        Console.WriteLine("⚠️ Seite nicht verfügbar");
                        break;
                    }

                    Console.WriteLine("\n");
                    Console.WriteLine("                      *** 🌐 GEIZHALS SUCHERGEBNISSE ***\n");

                    var elements = await page.QuerySelectorAllAsync("article.galleryview__item");

                    Console.WriteLine($"{pageIndex}. Seite --> {elements.Count} Produkte");
                    totalCount += elements.Count;

                    foreach (var el in elements)
                    {
                        try
                        {
                            var titleEl = await el.QuerySelectorAsync("a.galleryview__name-link");
                            var priceEl = await el.QuerySelectorAsync("span.gh_price, span[class*='price'], div[class*='price']");

                            var title = titleEl != null ? (await titleEl.InnerTextAsync()).Trim() : "";
                            var fullText = await el.InnerTextAsync();
                            var priceText = priceEl != null ? await priceEl.InnerTextAsync() : "";

                            if (string.IsNullOrWhiteSpace(title))
                                continue;

                            if (string.IsNullOrWhiteSpace(priceText) ||
                                fullText.ToLower().Contains("kein angebot") ||
                                fullText.ToLower().Contains("keine angebote") ||
                                fullText.ToLower().Contains("anfrage"))
                            {
                                filteredOut++;
                                continue;
                            }

                            double price = ParsePrice(priceText);
                            if (price <= 0) continue;

                            var produkt = new Produkt
                            {
                                name = title,
                                kategorie = "Geizhals",
                                bewertung = price
                            };

                            results.Add(produkt);

                            Console.WriteLine("-------------------------------------------------------------------------------");
                            Console.WriteLine($"{index}. {produkt.name}");
                            Console.WriteLine($"💰 {produkt.bewertung:0.00} €");
                            Console.WriteLine("🌐 GEIZHALS_AT");
                            Console.WriteLine("-------------------------------------------------------------------------------");

                            index++;
                        }
                        catch { }
                    }
                }

                await browser.CloseAsync();

                // ===============================
                // GEIZHALS STATISTIK
                // ===============================
                Console.WriteLine("\n");
                Console.WriteLine("=====================================================");
                Console.WriteLine($"✅ Gesamt gefunden:                           {totalCount}");
                Console.WriteLine($"❌ Gefiltert (Fehlende Preise  --> entfernt): {filteredOut}");
                Console.WriteLine($"✔ Verwendbare Ergebnisse:                     {results.Count}");
                Console.WriteLine("=====================================================");

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Fehler: {ex.Message}");
                return results;
            }
        }

        // ===============================
        // PRICE PARSER
        // ===============================
        private double ParsePrice(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+[.,]\d+");

            if (match.Success)
            {
                var cleaned = match.Value.Replace(",", ".");
                if (double.TryParse(cleaned,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double value))
                {
                    return value;
                }
            }

            return 0;
        }
    }
}