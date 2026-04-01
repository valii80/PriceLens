using System.Linq;
using System.Collections.Generic;

namespace PriceLens;

public class BewertungsService
{
    public List<Angebot> Rank(List<Angebot> angebote, string query)
    {
        var queryLower = query.ToLower();
        var queryWords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Wir nehmen den Median oder den Durchschnitt der teureren Hälfte, 
        // um den "echten" Preis des Produkts zu schätzen.
        var prices = angebote.Select(a => (double)a.preis).OrderByDescending(p => p).ToList();
        double topAverage = prices.Take(Math.Max(1, prices.Count / 10)).Average();

        foreach (var a in angebote)
        {
            var name = (a.produkt?.name ?? "").ToLower();
            var titleWords = name.Split(new[] { ' ', '/', '-', '|' }, StringSplitOptions.RemoveEmptyEntries);
            double score = 0;
            double preis = (double)a.preis;

            // 1. Wort-Übereinstimmung (Jedes Wort zählt!)
            int matches = queryWords.Count(qW => name.Contains(qW));
            score += matches * 500;

            // 2. Penalty für "Fremdwörter" (Verhindert Hüllen-Spam)
            int extraWords = titleWords.Length - matches;
            score -= extraWords * 50;

            // 3. Preis-Logik (Dynamisch für Handy ODER Chips)
            // Wenn ein Artikel weniger als 20% vom "Top-Schnitt" kostet -> Zubehör-Verdacht
            if (preis < (topAverage * 0.2))
            {
                score -= 2000;
            }
            else if (preis > (topAverage * 0.7))
            {
                score += 1000; // Das ist wahrscheinlich das Hauptgerät
            }

            // 4. Phrasen-Bonus (Hintereinander geschriebene Wörter)
            if (name.Contains(queryLower)) score += 500;

            a.score = (int)score;
        }

        return angebote.OrderByDescending(a => a.score).ToList();
    }

}