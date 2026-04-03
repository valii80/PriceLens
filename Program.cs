using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using PriceLens;

// ==================
// ENCODING & SETUP
// ==================
Console.OutputEncoding = System.Text.Encoding.UTF8;

var vergleichService = new VergleichsService();
var webScraper = new GenericShopScraper(new HttpClient());
var searchService = new SearchService();

// ===========
// HAUPTLOOP
// ===========
while (true)
{
    Console.Clear();

    // ========
    // HEADER
    // ========
    Console.WriteLine("   ╔═════════════════════════════════════════════════════════════════════════════════╗   ");
    Console.WriteLine("   ║             >>>>>>>>>>       🔍 PriceLens Search       <<<<<<<<<<               ║   ");
    Console.WriteLine("   ╚═════════════════════════════════════════════════════════════════════════════════╝   ");
    
    Console.WriteLine("   👉 Drücke ESC zum Beenden\n");

    var query = ReadEditableInput();
    if (query == null) break;
    if (string.IsNullOrWhiteSpace(query)) continue;

    // ============================================================
    // 2. DATEN LADEN GEIZHALS (DE+AT) + eBay --> parallel Tasking
    // ============================================================
    Console.WriteLine("\n⏳ Suche läuft parallel auf Geizhals (DE & AT) und eBay...");

    // Aufgaben (Tasks) gleichzeitig starten
    var taskDe = webScraper.ScrapeListAsync($"https://geizhals.de/?fs={Uri.EscapeDataString(query)}");
    var taskAt = webScraper.ScrapeListAsync($"https://geizhals.at/?fs={Uri.EscapeDataString(query)}");
    var taskEbay = searchService.Search(query);

    // ===================================
    // Warten, bis alle drei fertig sind
    // ===================================
    await Task.WhenAll(taskDe, taskAt, taskEbay);

    var ghDeResult = await taskDe;
    var ghAtResult = await taskAt;
    var ebayResult = await taskEbay;

    var alleAngebote = new List<DisplayItem>();

    // ========================
    // Geizhals.DE hinzufügen
    // ========================
    foreach (var p in ghDeResult.Produkte)
    {
        alleAngebote.Add(new DisplayItem { Name = p.name, Preis = p.bewertung, Shop = "GEIZHALS_DE", OriginalObject = p });
    }

    // ========================
    // Geizhals.AT hinzufügen
    // ========================
    foreach (var p in ghAtResult.Produkte)
    {
        alleAngebote.Add(new DisplayItem { Name = p.name, Preis = p.bewertung, Shop = "GEIZHALS_AT", OriginalObject = p });
    }

    // ===========================
    // eBay hinzufügen (Top 300) --> Mehr laden könnte zu lange dauern und unübersichtlich sein
    // ===========================
    foreach (var a in ebayResult.ranked.Take(300))
    {
        alleAngebote.Add(new DisplayItem { Name = a.produkt?.name ?? "Unbekannt", Preis = (double)a.preis, Shop = a.shop?.name ?? "EBAY", OriginalObject = a });
    }

    // ======================
    // SUCHERGEBNIS-ANZEIGE
    // ======================
    Console.Clear();
    
    Console.WriteLine("   ╔═════════════════════════════════════════════════════════════════════════════════╗   ");
    Console.WriteLine("   ║             >>>>>>>>>>       🔍 PriceLens Search       <<<<<<<<<<               ║   ");
    Console.WriteLine("   ╚═════════════════════════════════════════════════════════════════════════════════╝   ");
    
    // =====================================================
    // SYSTEM-STATUS (GenericShopScraper - Logs)
    // =====================================================
    Console.WriteLine("\n  ⚙️ SYSTEMSTATUS 👇");
    Console.WriteLine("-----------------------------------------------------------------------------------------");
    if (ghDeResult.Logs != null) foreach (var log in ghDeResult.Logs) Console.WriteLine($"  {log} (DE)");
    if (ghAtResult.Logs != null) foreach (var log in ghAtResult.Logs) Console.WriteLine($"  {log} (AT)");

    Console.WriteLine("  ✅ Suche abgeschlossen (📋 Die neue Liste steht bereit)");
    Console.WriteLine("-----------------------------------------------------------------------------------------");

    Console.WriteLine("\n                              *** 🔎 SUCHERGEBNISSE ***");
    Console.WriteLine("=========================================================================================\n");
    Console.WriteLine("_________________________________________________________________________________________");
    
    // ===============
    // ARTIKEL LISTE
    // ===============
    for (int i = 0; i < alleAngebote.Count; i++)
    {
        var item = alleAngebote[i];

       /* Console.ForegroundColor = item.Shop switch
        {
            "GEIZHALS_DE" => ConsoleColor.Cyan,
            "GEIZHALS_AT" => ConsoleColor.Cyan,
            "EBAY" => ConsoleColor.Cyan,
            _ => ConsoleColor.Cyan
        };*/
       
        Console.WriteLine($"{i + 1,3}. {item.Name}");
        Console.WriteLine("-----------------------------------------------------------------------------------------");
        Console.WriteLine($"   |💰 {item.Preis:0.00} €");
        Console.WriteLine($"   |🌐 {item.Shop}");
        Console.WriteLine("");
        Console.WriteLine("_________________________________________________________________________________________");
        //Console.ResetColor();  
    }

    // ==================
    // SHOP STATISTIKEN
    // ==================
    int totalGhGefunden = (ghDeResult?.TotalGefunden ?? 0) + (ghAtResult?.TotalGefunden ?? 0);
    int totalGhGefiltert = (ghDeResult?.Gefiltert ?? 0) + (ghAtResult?.Gefiltert ?? 0);
    Console.WriteLine("=========================================================================================");
    Console.WriteLine($"|🌐 GEIZHALS:      {totalGhGefunden,-4} Artikel gefunden         | Gesamt (DE+AT)");
    Console.WriteLine($"|🌐 GEIZHALS:      {totalGhGefiltert,-4} Artikel entfernt         | Keine Angebote (Preis fehlt)");
    Console.WriteLine($"|🌐 EBAY:          {ebayResult.raw.Count,-4} Artikel gefunden         | Gesamt gefundene Artikel");
    Console.WriteLine($"|🌐 EBAY:          {ebayResult.ranked.Count,-4} Artikel sortiert         | Relevante Ergebnisse");
    Console.WriteLine($"|📋 AUFGELISTET:   {alleAngebote.Count,-4} Artikel gesamt           | Geizhals + Ebay");
    Console.WriteLine("=========================================================================================\n");

    // ================================
    // KI-ASSISTENT ARTIKEL-VERGLEICH
    // ================================
    Console.WriteLine("👉 (2) ARTIKEL-Vergleich starten (z.B. 9 67 + ENTER) und/oder ENTER für neue Suche:");
    var input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input))
    {
        var parts = input.Split(' ');
        if (parts.Length == 2 && int.TryParse(parts[0], out int i1) && int.TryParse(parts[1], out int i2))
        {
            if (i1 > 0 && i2 > 0 && i1 <= alleAngebote.Count && i2 <= alleAngebote.Count)
            {
                Console.WriteLine("\n🤖 KI-Assistent analysiert... ⏳");
                var text = await vergleichService.VergleicheAsync(alleAngebote[i1 - 1], alleAngebote[i2 - 1]);
                Console.WriteLine("\n" + text);
            }
        }
    }

    Console.WriteLine("\n[ENTER] Neue Suche | [ESC] Beenden");
    if (Console.ReadKey(true).Key == ConsoleKey.Escape) break;
}

// ============================
// INPUT MIT CURSOR STEUERUNG
// ============================
static string? ReadEditableInput()
{
    var input = "";
    int cursor = 0;
    string prompt = " 👉 Artikel suchen: ";

    // ==================
    // BENUTZER-HINWEIS
    // ==================
    Console.WriteLine("---------------------------------------------------------------------------------------");
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine(" ⚠️ WICHTIGER HINWEIS: Bitte achte auf die korrekte Schreibweise deines Suchbegriffs!\n");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("          Bitte prüfe die Eingabe nochmal, falls nötig bitte korrigieren.\n");
    Console.ResetColor();
    Console.WriteLine("---------------------------------------------------------------------------------------");

    Console.Write(prompt);

    while (true)
    {
        var key = Console.ReadKey(true);
        
        // --- ESCAPE TASTE ABFANGEN ---
        if (key.Key == ConsoleKey.Escape)
        
        {
            return null; // Signalisiert dem Hauptprogramm: "Beenden!"
        }

        // --- ENTER ---
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            if (string.IsNullOrWhiteSpace(input)) return input;

            // --- Google-Style Normalisierung (für Eingabefehler) ---
            var words = input.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cleanWords = words.Select(w =>
                System.Text.RegularExpressions.Regex.Replace(w, @"(.)\1+", "$1")
            );

            return string.Join(" ", cleanWords);
        }

        if (key.Key == ConsoleKey.Backspace && cursor > 0){input = input.Remove(cursor - 1, 1);cursor--;}
        else if (key.Key == ConsoleKey.LeftArrow && cursor > 0) { cursor--; }
        else if (key.Key == ConsoleKey.RightArrow && cursor < input.Length) { cursor++; }
        else if (!char.IsControl(key.KeyChar) || key.Key == ConsoleKey.Spacebar){input = input.Insert(cursor, key.KeyChar.ToString());
            
            cursor++;
        }

        Console.CursorLeft = 0;
        Console.Write(prompt + input + "   ");
        Console.CursorLeft = prompt.Length + cursor;
    }
}
