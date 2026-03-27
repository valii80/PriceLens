namespace PriceLens;

public class VergleichsService
{
    // Startet den Vergleich von Angeboten und liefert Ergebnisse zurück
    public List<VergleichsErgebnis> Vergleiche(List<Angebot> angebote)
    {
        var ergebnisse = new List<VergleichsErgebnis>();

        if (angebote == null || angebote.Count == 0)
            return ergebnisse;

        var gruppen = angebote
            .GroupBy(a => a.produkt?.name ?? "");

        foreach (var gruppe in gruppen)
        {
            var liste = gruppe.ToList();

            decimal summe = liste.Sum(a => a.preis);
            decimal durchschnitt = summe / liste.Count;
            decimal besterPreis = liste.Min(a => a.preis);

            var ergebnis = new VergleichsErgebnis
            {
                produkt = liste[0].produkt,
                durchschnittspreis = durchschnitt,
                besterPreis = besterPreis
            };

            ergebnisse.Add(ergebnis);
        }

        return ergebnisse;
    }
}
