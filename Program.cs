using System.Collections.Generic;
using System.Linq;
using PriceLens;

// ===============================
// ENCODING
// ===============================
Console.OutputEncoding = System.Text.Encoding.UTF8;

// ===============================
// SERVICES INITIALISIEREN
// ===============================
var scraperManager = new ScraperManager();
var filterService = new FilterService();
var bewertungsService = new BewertungsService();
var vergleichService = new VergleichsService();

var webScraper = new GenericShopScraper(new HttpClient());

// ===============================
// HAUPTLOOP
// ===============================
while (true)
{
    Console.WriteLine("============ PriceLens ============");
    Console.WriteLine("ESC = Beenden");
    Console.WriteLine();

    // ===============================
    // USER INPUT
    // ===============================
    var query = ReadEditableInput();

    if (string.IsNullOrWhiteSpace(query))
        continue;

    // ===============================
    // WEB SCRAPER
    // ===============================
    string encodedQuery = Uri.EscapeDataString(query);
    string url = $"https://geizhals.de/?fs={encodedQuery}";
    
    var webResults = await webScraper.ScrapeListAsync(url);

    // ===============================
    // EBAY DATEN LADEN
    // ===============================
    Console.WriteLine("\n");
    Console.WriteLine("\n");
    Console.WriteLine("                    *** 🌐 EBAY API SUCHERGEBNISSE ***");
    Console.WriteLine("\n");

    var searchService = new SearchService();
    var result = await searchService.Search(query);

    var allResults = result.raw;
    var gefiltert = result.gefiltert;
    var cleaned = result.cleaned;
    var ranked = result.ranked;
    int index = 1;

    foreach (var a in ranked.Take(100))
    {
        Console.WriteLine("-------------------------------------------------------------------------------");
        Console.WriteLine($"{index}. {a.produkt?.name ?? "Unbekannt"}");
        Console.WriteLine($"💰 {a.preis} {a.waehrung}");
        Console.WriteLine($"🌐 {a.shop?.name}");
        Console.WriteLine("-------------------------------------------------------------------------------");
        
        index++;
    }

    // ===============================
    // EBAY STATISTIK
    // ===============================
    Console.WriteLine();
    Console.WriteLine("==================================================");
    Console.WriteLine($"✅ Gesamt gefunden:          {allResults.Count}");
    Console.WriteLine($"⏳ Sortiert nach Relevanz:   {gefiltert.Count}");
    Console.WriteLine($"✔  Verwendbare Ergebnisse:   {ranked.Count}");
    Console.WriteLine("==================================================");
    Console.WriteLine("\n");

    // ===============================
    // VERGLEICH
    // ===============================
    Console.WriteLine();
    Console.WriteLine("👉 Vergleich starten (z.B. 1 2 + ENTER oder ENTER zum Überspringen):");

    var input = Console.ReadLine();

    if (!string.IsNullOrWhiteSpace(input))
    {
        var parts = input.Split(' ');

        int i1, i2;

        if (parts.Length == 1 && int.TryParse(parts[0], out i2))
        {
            i1 = 1;
        }
        else if (parts.Length == 2 &&
                 int.TryParse(parts[0], out i1) &&
                 int.TryParse(parts[1], out i2))
        {
        }
        else
        {
            Console.WriteLine("❌ Ungültige Eingabe");
            continue;
        }

        if (i1 > 0 && i2 > 0 &&
            i1 <= ranked.Count &&
            i2 <= ranked.Count)
        {
            var text = await vergleichService.VergleicheAsync(
                ranked[i1 - 1],
                ranked[i2 - 1]
            );

            Console.WriteLine();
            Console.WriteLine("🤖 KI Vergleich:");
            Console.WriteLine("----------------------------------");
            Console.WriteLine(text);
        }
        else
        {
            Console.WriteLine("❌ Index außerhalb Bereich");
        }
    }

    Console.WriteLine();
    Console.WriteLine("ENTER (Neue Suche starten) oder --> ESC (Beenden)...");

    var key = Console.ReadKey(true);

    if (key.Key == ConsoleKey.Escape)
        break;

    Console.Clear();
}

// ===============================
// INPUT MIT CURSOR STEUERUNG
// ===============================
static string ReadEditableInput()
{
    var input = "";
    int cursor = 0;

    Console.Write("Artikel suchen: ");

    while (true)
    {
        var key = Console.ReadKey(true);

        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return input;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (cursor > 0 && input.Length > 0)
            {
                input = input.Remove(cursor - 1, 1);
                cursor--;
            }
        }
        else if (key.Key == ConsoleKey.LeftArrow && cursor > 0)
        {
            cursor--;
        }
        else if (key.Key == ConsoleKey.RightArrow && cursor < input.Length)
        {
            cursor++;
        }
        else if (!char.IsControl(key.KeyChar))
        {
            input = input.Insert(cursor, key.KeyChar.ToString());
            cursor++;
        }

        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write("Artikel suchen: " + input + " ");
        Console.SetCursorPosition("Artikel suchen: ".Length + cursor, Console.CursorTop);
    }
}