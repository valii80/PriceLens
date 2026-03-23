using System.Net;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using PriceLens;

public class TestScraper : IScraper
{
    public async Task<List<Angebot>> ScrapeAsync(string suchbegriff)
    {
        var angebote = new List<Angebot>();

        var options = new ChromeOptions();
        // KEIN headless → wir wollen sehen was passiert

        using var driver = new ChromeDriver(options);

        // einfache Test-Seite (kein Block!)    
        var url = $"https://www.ebay.de/sch/i.html?_nkw={WebUtility.UrlEncode(suchbegriff)}";
        driver.Navigate().GoToUrl(url);

        // erst Consent schließen (kurzes Timeout)
        try
        {
            var shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
            var accept = shortWait.Until(d =>
                d.FindElements(By.CssSelector("button, a"))
                 .FirstOrDefault(e => (e.Text ?? "").ToLower().Contains("akzeptieren")
                                     || (e.GetAttribute("id") ?? "").ToLower().Contains("consent")
                                     || (e.GetAttribute("aria-label") ?? "").ToLower().Contains("accept")));
            accept?.Click();
            Thread.Sleep(500);
        }
        catch (WebDriverTimeoutException) { /* kein Consent gefunden, weiter */ }

        // dann robust auf Items warten (länger)
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
        wait.Until(d => d.FindElements(By.CssSelector("li.s-item, .s-item")).Count > 5);

        var items = driver.FindElements(By.CssSelector(".s-item"));

        Console.WriteLine("Gefundene Elemente: " + items.Count);

        foreach (var item in items)
        {
            try
            {
                var title = item.FindElement(By.CssSelector(".s-item__title")).Text;

                var priceText = item.FindElement(By.CssSelector(".s-item__price")).Text;

                var price = ExtractPrice(priceText);

                if (price <= 0) continue;

                angebote.Add(new Angebot
                {
                    preis = price,
                    waehrung = "EUR",
                    produkt = new Produkt { name = title },
                    shop = new Shop { name = "eBay" }
                });
            }
            catch
            {
                // ignorieren
            }
        }

        driver.Quit();

        return angebote;
    }

    private decimal ExtractPrice(string text)
    {
        // Beispiel: £51.77
        var clean = text.Replace("£", "").Trim();

        return decimal.TryParse(clean, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }
}