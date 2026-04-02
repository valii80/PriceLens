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
var vergleichService = new VergleichsService();
var webScraper = new GenericShopScraper(new HttpClient());

// ===============================
// HAUPTLOOP
// ===============================
while (true)
{
    Console.Clear();

    // ================
    // HEADER
    // ================
    Console.WriteLine("╔═════════════════════════════════════════════════╗");
    Console.WriteLine("║               🔍 PRICELENS SEARCH               ║");
    Console.WriteLine("╚═════════════════════════════════════════════════╝");
    Console.WriteLine(" › Drücke ESC zum Beenden");
    Console.WriteLine();

    var query = ReadEditableInput();
    if (string.IsNullOrWhiteSpace(query)) continue;

    // ================
    // DATEN LADEN
    // ================
    Console.WriteLine("\n ⏳ Suche läuft auf Geizhals und eBay...");

    var ghResult = await webScraper.ScrapeListAsync($"https://geizhals.de/?fs={Uri.EscapeDataString(query)}");
    var searchService = new SearchService();
    var ebayResult = await searchService.Search(query);

    var alleAngebote = new List<DisplayItem>();

    foreach (var p in ghResult.Produkte)
    {
        alleAngebote.Add(new DisplayItem { 
            Name = p.name, 
            Preis = p.bewertung, 
            Shop = "GEIZHALS_AT", 
            OriginalObject = p });
    }

    foreach (var a in ebayResult.ranked.Take(300))
    {
        alleAngebote.Add(new DisplayItem { 
            Name = a.produkt?.name ?? "Unbekannt", 
            Preis = (double)a.preis, 
            Shop = a.shop?.name ?? "EBAY", 
            OriginalObject = a });
    }

    // ============================
    // EINHEITLICHE DARSTELLUNG
    // ============================
    Console.Clear();
    Console.WriteLine("\n-----------------------------------------------------------------------------------------");
    Console.WriteLine("                            *** 🌐 ALLE SUCHERGEBNISSE ***");
    Console.WriteLine("=========================================================================================\n");

    // =======================
    // ARTIKEL AUFLISTUNG
    // =======================
    for (int i = 0; i < alleAngebote.Count; i++)
    {
        var item = alleAngebote[i];

        Console.WriteLine($"{i + 1}. {item.Name}");
        Console.WriteLine($"💰 {item.Preis:0.00} €");
        Console.WriteLine($"🌐 {item.Shop}");
        Console.WriteLine("-----------------------------------------------------------------------------------------");
    }

    // STATISTIKEN
    Console.WriteLine("=========================================================================================");
    Console.WriteLine($"|🌐 GEIZHALS:      {ghResult.TotalGefunden,-4} Artikel gefunden         | Gesamt");
    Console.WriteLine($"|🌐 GEIZHALS:      {ghResult.Gefiltert,-4} Artikel entfernt         | Keine Angebote (Preis fehlt)");
    Console.WriteLine($"|🌐 EBAY:          {ebayResult.raw.Count,-4} Artikel gefunden         | Gesamt gefundene Artikel");
    Console.WriteLine($"|🌐 EBAY:          {ebayResult.ranked.Count,-4} Artikel sortiert         | Relevante Ergebnisse");
    Console.WriteLine($"|📋 AUFGELISTET:   {alleAngebote.Count,-4} Artikel gesamt           | Geizhals + Ebay");
    Console.WriteLine("=========================================================================================\n");

    // ===============================
    // VERGLEICH
    // ===============================
    Console.WriteLine();
    Console.WriteLine("👉(2)ARTIKEL-Vergleich starten --> (z.B. 14 36 + ENTER) oder ENTER zum Überspringen:");

    var input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input))
    {
        var parts = input.Split(' ');
        if (parts.Length == 2 && int.TryParse(parts[0], out int i1) && int.TryParse(parts[1], out int i2))
        {
            if (i1 > 0 && i2 > 0 && i1 <= alleAngebote.Count && i2 <= alleAngebote.Count)
            {
                Console.WriteLine("\n🤖 KI-Assistent analisysiert die Angebote...⏳");
                var text = await vergleichService.VergleicheAsync(alleAngebote[i1 - 1], alleAngebote[i2 - 1]);

                // DEINE GELIEBTE KI-BOX
                Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                    🤖 KI VERGLEICHSBERICHT                 ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
                Console.WriteLine(text);
            }
            else Console.WriteLine("❌ Ungültiger Index!");
        }
    }

    Console.WriteLine("\n[ENTER] Neue Suche | [ESC] Beenden");
    var keyInfo = Console.ReadKey(true);
    if (keyInfo.Key == ConsoleKey.Escape) break;
}

// ===============================
// INPUT MIT CURSOR STEUERUNG
// ===============================
static string ReadEditableInput()
{
    var input = "";
    int cursor = 0;
    Console.Write(" Artikel suchen: ");

    while (true)
    {
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return input; }
        if (key.Key == ConsoleKey.Backspace && cursor > 0) { input = input.Remove(cursor - 1, 1); cursor--; }
        else if (key.Key == ConsoleKey.LeftArrow && cursor > 0) cursor--;
        else if (key.Key == ConsoleKey.RightArrow && cursor < input.Length) cursor++;
        else if (!char.IsControl(key.KeyChar)) { input = input.Insert(cursor, key.KeyChar.ToString()); cursor++; }

        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(" Artikel suchen: " + input);
        Console.SetCursorPosition(" Artikel suchen: ".Length + cursor, Console.CursorTop);
    }
}