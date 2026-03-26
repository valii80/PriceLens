using PriceLens;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("============  PriceLens ============");
Console.WriteLine();

var client = new EbayApiClient();

Console.Write("Artikel suchen: ");
var query = Console.ReadLine() ?? "";

// API-Daten abrufen
var de = await client.SearchAsync(query, "EBAY_DE");
var us = await client.SearchAsync(query, "EBAY_US");
var uk = await client.SearchAsync(query, "EBAY_GB");

// 🔥 ALLES ZUSAMMEN
var angebote = de
    .Concat(us)
    .Concat(uk)
    .GroupBy(a => a.produkt?.name ?? "")
    .Select(g => g.First())
    .ToList();

// Preis sortiert (Aufsteigend)
angebote = angebote
    .OrderBy(a => a.preis)
    .ToList();

// Filter anwenden, Filter-Klasse verwenden
var filterService = new FilterService();
var gefiltert = filterService.Filter(angebote, query);

// Anzeigen der gefilterten und ungefilterten Angebote
Console.WriteLine();
Console.WriteLine($"Gefunden: {angebote.Count}");
Console.WriteLine("==================================================");

foreach (var a in angebote.Take(5))
{
    Console.WriteLine($"{a.produkt?.name}");
    Console.WriteLine($"{a.preis} {a.waehrung} |{a.shop?.name}");
    Console.WriteLine("--------------------------------------------------");
}
Console.WriteLine();
Console.WriteLine("==============================================");
Console.WriteLine();

Console.WriteLine($"Gefiltert: {gefiltert.Count}");
Console.WriteLine("==================================================");

foreach (var a in gefiltert.Take(5))
{
    Console.WriteLine($"{a.produkt?.name}");
    Console.WriteLine($"{a.preis} {a.waehrung} |{a.shop?.name}");
    Console.WriteLine("--------------------------------------------------");
}
return;