using PriceLens;

public class VergleichsService
{
    private readonly GeminiService gemini = new();

    public async Task<string> VergleicheAsync(Angebot a1, Angebot a2)
    {
        var p1 = a1.produkt?.name ?? "Unbekannt";
        var p2 = a2.produkt?.name ?? "Unbekannt";

        var prompt = $@"
Du bist ein Produktexperte.

Vergleiche die folgenden zwei Produkte objektiv und praxisnah:

PRODUKT 1:
Name: {p1}
Preis: {a1.preis} {a1.waehrung}

PRODUKT 2:
Name: {p2}
Preis: {a2.preis} {a2.waehrung}

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