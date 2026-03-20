using PriceLens;

IScraper scraper = new FakeScraper();

// Angebote vom Scraper holen
var angebote = await scraper.ScrapeAsync("iPhone 15");

// Vergleich starten
VergleichsService service = new VergleichsService();
var ergebnisse = service.Vergleiche(angebote);

// Ausgabe
foreach (var e in ergebnisse)
{
    Console.WriteLine($"Produkt: {e.produkt?.name}");
    Console.WriteLine($"Durchschnitt: {e.durchschnittspreis}");
    Console.WriteLine($"Bester Preis: {e.besterPreis}");
    Console.WriteLine("------");
}