using PriceLens;
using System.Collections.Generic;
using System.Linq;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ===============================
// SERVICES INITIALISIEREN
// ===============================
var scraperManager = new ScraperManager();
var filterService = new FilterService();
var bewertungsService = new BewertungsService();
var vergleichService = new VergleichsService();

// ===============================
// HAUPTLOOP
// ===============================
while (true)
{
    // ===============================
    // HEADER
    // ===============================
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
    // DATEN LADEN (über ScraperManager!)
    // ===============================
    var allResults = await scraperManager.LadeDaten(query);

    // ===============================
    // DUPLIKATE ENTFERNEN
    // ===============================
    var angebote = allResults
    .GroupBy(a => a.produkt?.name ?? "")
    .Select(g => g.First())
    .ToList();

    // ===============================
    // FILTER (Relevanz)
    // ===============================
    var gefiltert = filterService.Filter(angebote, query);

    // ===============================
    // RANKING (Score berechnen)
    // ===============================
    var ranked = bewertungsService.Rank(gefiltert, query);
    Console.WriteLine($"DEBUG ranked: {ranked.Count}");

    // ===============================
    // AUSGABE LISTE
    // ===============================
    Console.WriteLine();
    Console.WriteLine($"Gefunden (roh): {allResults.Count}");
    Console.WriteLine($"Nach Dedup: {angebote.Count}");
    Console.WriteLine($"Gefiltert (relevant): {gefiltert.Count}");
    Console.WriteLine("==========================================");

    int index = 1;

    foreach (var a in ranked.Take(10))
    {
        Console.WriteLine($"{index}. {a.produkt?.name ?? "Unbekannt"}");
        Console.WriteLine($"💰 {a.preis} {a.waehrung}");
        Console.WriteLine($"🏪 {a.shop?.name}");
        Console.WriteLine("----------------------------------");
        index++;
    }

    // ===============================
    // VERGLEICH STARTEN
    // ===============================
    Console.WriteLine();
    Console.WriteLine("👉 Vergleich starten? (z.B. 1 2) oder ENTER zum Überspringen:");

    var input = Console.ReadLine();

    if (!string.IsNullOrWhiteSpace(input))
    {
        var parts = input.Split(' ');

        int i1, i2;

        // FALL 1: Nur eine Zahl → vergleiche mit Top 1
        if (parts.Length == 1 && int.TryParse(parts[0], out i2))
        {
            i1 = 1;
        }
        // FALL 2: Zwei Zahlen
        else if (parts.Length == 2 &&
                 int.TryParse(parts[0], out i1) &&
                 int.TryParse(parts[1], out i2))
        {
            // ok
        }
        else
        {
            Console.WriteLine("❌ Ungültige Eingabe");
            continue;
        }

        // ===============================
        // VALIDIERUNG
        // ===============================
        if (i1 > 0 && i2 > 0 &&
            i1 <= ranked.Count &&
            i2 <= ranked.Count)
        {
            // ===============================
            // KI VERGLEICH
            // ===============================
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

    // ===============================
    // LOOP STEUERUNG
    // ===============================
    Console.WriteLine();
    Console.WriteLine("Neue Suche starten oder --> ESC (Beenden)...");

    var key = Console.ReadKey(true);

    if (key.Key == ConsoleKey.Escape)
        break;

    Console.Clear();
}

// ===============================
// EDITIERBARE INPUT-METHODE
// ===============================
static string ReadEditableInput()
{
    var input = "";
    int cursor = 0;

    Console.Write("Artikel suchen: ");

    while (true)
    {
        var key = Console.ReadKey(true);

        // ENTER → fertig
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return input;
        }

        // BACKSPACE
        if (key.Key == ConsoleKey.Backspace)
        {
            if (cursor > 0 && input.Length > 0)
            {
                input = input.Remove(cursor - 1, 1);
                cursor--;
            }
        }
        // LINKS
        else if (key.Key == ConsoleKey.LeftArrow && cursor > 0)
        {
            cursor--;
        }
        // RECHTS
        else if (key.Key == ConsoleKey.RightArrow && cursor < input.Length)
        {
            cursor++;
        }
        // TEXT
        else if (!char.IsControl(key.KeyChar))
        {
            input = input.Insert(cursor, key.KeyChar.ToString());
            cursor++;
        }

        // REDRAW
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write("Artikel suchen: " + input + " ");
        Console.SetCursorPosition("Artikel suchen: ".Length + cursor, Console.CursorTop);
    }
}