using PriceLens;

public class VergleichsService
{
    private readonly GeminiService gemini = new();

    // Wir übergeben jetzt zwei DisplayItems statt zwei Angebote
    public async Task<string> VergleicheAsync(DisplayItem item1, DisplayItem item2)
    {
        var prompt = $@"
Du bist ein Produktexperte.

Vergleiche die folgenden zwei Produkte objektiv und praxisnah:

PRODUKT 1:
Name: {item1.Name}
Preis: {item1.Preis:0.00} EUR
Quelle: {item1.Shop}

PRODUKT 2:
Name: {item2.Name}
Preis: {item2.Preis:0.00} EUR
Quelle: {item2.Shop}

Analysiere strukturiert:

1. Produktart erkennen:
- Sind es gleiche Kategorien?
- Wenn NEIN → erkläre kurz warum kein direkter Vergleich möglich ist

2. Preis-Leistung:
- Welches bietet mehr fürs Geld?

3. Qualität:
- Typische Qualität dieser Produktart
- Materialien / Verarbeitung (realistisch einschätzen)

4. Nutzerbewertungen (geschätzt):
- Realistische Sternebewertung (z.B. 4.2/5)
- Kurz begründen

5. Einsatzbereich:
- Für wen geeignet?
- Alltag / Spezialfall / Reparatur etc.

6. Vorteile & Nachteile:
- Produkt 1 (Bulletpoints)
- Produkt 2 (Bulletpoints)

7. Klare Empfehlung:
- Welches Produkt ist besser?
- Für welchen Nutzer?

WICHTIG:
- Antworte klar, strukturiert, übersichtlich
- Kein unnötiger Text
- Wenn Produkte nicht vergleichbar sind → deutlich sagen
";

        return await gemini.GenerateComparison(prompt);
    }
}