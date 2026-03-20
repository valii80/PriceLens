using PriceLens;

namespace PriceLens;

// FakeScraper simuliert eine echte Datenquelle (z.B. Website)
// → liefert Testdaten statt echte Internet-Daten
public class FakeScraper : IScraper
{
    // Diese Methode simuliert das "Scrapen" von Angeboten
    // suchbegriff = z.B. "iPhone 15"
    public Task<List<Angebot>> ScrapeAsync(string suchbegriff)
    {
        // Liste für alle gefundenen Angebote
        var angebote = new List<Angebot>();

        // Produkt wird einmal erstellt (alle Angebote beziehen sich darauf)
        var produkt = new Produkt
        {
            name = suchbegriff,      // Suchbegriff wird als Produktname verwendet
            kategorie = "Test",      // Dummy-Kategorie

            // Bewertung wird zufällig zwischen 3.5 und 5.0 generiert (realistisch)
            bewertung = 3.5 + Random.Shared.NextDouble() * 1.5
        };

        // Shops simulieren (verschiedene Anbieter)
        var shop1 = new Shop { name = "Amazon", url = "amazon.de" };
        var shop2 = new Shop { name = "eBay", url = "ebay.de" };
        var shop3 = new Shop { name = "MediaMarkt", url = "mediamarkt.de" };
        var shop4 = new Shop { name = "Saturn", url = "saturn.de" };
        var shop5 = new Shop { name = "Otto", url = "otto.de" };

        // Angebote hinzufügen (verschiedene Preise für das gleiche Produkt)
        angebote.Add(new Angebot { preis = 999, waehrung = "EUR", produkt = produkt, shop = shop1 });
        angebote.Add(new Angebot { preis = 950, waehrung = "EUR", produkt = produkt, shop = shop2 });
        angebote.Add(new Angebot { preis = 980, waehrung = "EUR", produkt = produkt, shop = shop3 });
        angebote.Add(new Angebot { preis = 970, waehrung = "EUR", produkt = produkt, shop = shop4 });
        angebote.Add(new Angebot { preis = 965, waehrung = "EUR", produkt = produkt, shop = shop5 });

        // Rückgabe als Task → simuliert asynchrones Verhalten (wie echtes Web-Scraping)
        return Task.FromResult(angebote);
    }
}