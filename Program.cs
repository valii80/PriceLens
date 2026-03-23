using PriceLens;

Console.WriteLine("=== PriceLens ===");

while (true)
{
    Console.Write("\nSuchbegriff eingeben (oder 'exit'): ");
    var suchbegriff = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(suchbegriff))
        continue;

    if (suchbegriff.ToLower() == "exit")
        break;

    IScraper scraper = new TestScraper();

    var angebote = await scraper.ScrapeAsync(suchbegriff);

    Console.WriteLine($"\nAngebote gefunden: {angebote.Count}");

    foreach (var a in angebote.Take(5))
    {
        Console.WriteLine($"{a.produkt?.name} | {a.preis}€");
    }
}