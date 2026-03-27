using PriceLens;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var client = new EbayApiClient();

// HAUPTLOOP → läuft bis ESC gedrückt wird
while (true)
{
    // ===============================
    // HEADER
    // ===============================
    Console.WriteLine("============  PriceLens  ============");
    Console.WriteLine("ESC = Beenden");
    Console.WriteLine();

    // ===============================
    // INPUT
    // ===============================
    var query = ReadEditableInput();

    // Wenn leer → nächste Runde
    if (string.IsNullOrWhiteSpace(query))
        continue;

    // ===============================
    // QUERY VARIANTEN ERZEUGEN
    // ===============================
    var words = query
        .ToLower()
        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

    var queries = new List<string>();

    // Original Query
    queries.Add(query);

    // Varianten (je 1 Wort entfernen)
    for (int i = 0; i < words.Length; i++)
    {
        var reduced = words.Where((w, index) => index != i);
        var newQuery = string.Join(" ", reduced);

        if (!string.IsNullOrWhiteSpace(newQuery))
            queries.Add(newQuery);
    }

    // ===============================
    // API AUFRUFE
    // ===============================
    var allResults = new List<Angebot>();

    foreach (var q in queries.Distinct())
    {
        var de = await client.SearchAsync(q, "EBAY_DE");
        var us = await client.SearchAsync(q, "EBAY_US");
        var uk = await client.SearchAsync(q, "EBAY_GB");

        allResults.AddRange(de);
        allResults.AddRange(us);
        allResults.AddRange(uk);
    }

    // ===============================
    // DUPLIKATE ENTFERNEN
    // ===============================
    var angebote = allResults
        .GroupBy(a => a.produkt?.name ?? "")
        .Select(g => g.First())
        .ToList();

    // ===============================
    // KEINE SORTIERUNG HIER!
    // → Program.cs zeigt nur Daten
    // ===============================

    // ===============================
    // FILTER / RANKING (externe Logik)
    // ===============================
    var filterService = new FilterService();
    var gefiltert = filterService.Filter(angebote, query);

    // ===============================
    // AUSGABE (ROHDATEN)
    // ===============================
    Console.WriteLine();
    Console.WriteLine($"Gefunden (roh): {angebote.Count}");
    Console.WriteLine("==================================================");

    foreach (var a in angebote.Take(5))
    {
        Console.WriteLine($"📦 {a.produkt?.name ?? "Unbekannt"}");
        Console.WriteLine($"💰 {a.preis} {a.waehrung}");
        Console.WriteLine($"🏪 {a.shop?.name}");
        Console.WriteLine("--------------------------------------------------");
    }

    // ===============================
    // AUSGABE (INTELLIGENTE ERGEBNISSE)
    // ===============================
    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine();

    Console.WriteLine($"Gefiltert (relevant): {gefiltert.Count}");
    Console.WriteLine("==================================================");

    foreach (var a in gefiltert.Take(5))
    {
        Console.WriteLine($"📦 {a.produkt?.name ?? "Unbekannt"}");
        Console.WriteLine($"💰 {a.preis} {a.waehrung}");
        Console.WriteLine($"🏪 {a.shop?.name}");
        Console.WriteLine("--------------------------------------------------");
    }

    // ===============================
    // WEITER ODER EXIT
    // ===============================
    Console.WriteLine();
    Console.WriteLine("Neue Suche starten oder --> ESC (Beenden)...");

    var key = Console.ReadKey(true);

    if (key.Key == ConsoleKey.Escape)
        break;
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

        // 🔁 REDRAW
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write("Artikel suchen: " + input + " ");
        Console.SetCursorPosition("Artikel suchen: ".Length + cursor, Console.CursorTop);
    }
}