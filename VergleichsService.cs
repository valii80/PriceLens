namespace PriceLens;

public class VergleichsService
{
    // Startet den Vergleich von Angeboten und liefert Ergebnisse zurück
    public List<VergleichsErgebnis> Vergleiche(List<Angebot> angebote)
    {

        // Ergebnisliste vorbereiten
        var ergebnisse = new List<VergleichsErgebnis>();


        // Falls keine Angebote vorhanden sind → leere Liste zurückgeben
        if (angebote == null || angebote.Count == 0)
            return ergebnisse;


        // Summe aller Preise berechnen
        decimal summe = 0;

        foreach (var angebot in angebote)
        {
            summe += angebot.preis;
        }


        // Durchschnitt berechnen
        decimal durchschnitt = summe / angebote.Count;


        // Besten (günstigsten) Preis finden
        decimal besterPreis = angebote[0].preis;

        foreach (var angebot in angebote)
        {
            if (angebot.preis < besterPreis)
            {
                besterPreis = angebot.preis;
            }
        }


        // VergleichsErgebnis Objekt erstellen
        VergleichsErgebnis ergebnis = new VergleichsErgebnis();

        // Werte zuweisen
        ergebnis.durchschnittspreis = durchschnitt;
        ergebnis.besterPreis = besterPreis;
        ergebnis.produkt = angebote[0].produkt;

        // Ergebnis zur Liste hinzufügen
        ergebnisse.Add(ergebnis);

        return ergebnisse;
    }
}
