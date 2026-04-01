using PriceLens;
using System.Collections.Generic;
using System.Linq;

namespace PriceLens
{
    public class FilterService
    {
        public List<Angebot> Filter(List<Angebot> angebote, string query)
        {
            var queryWords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return angebote.Where(a =>
            {
                var name = (a.produkt?.name ?? "").ToLower();
                // Zähle, wie viele Wörter der Suche vorkommen
                int matchCount = queryWords.Count(w => name.Contains(w));

                // Wenn du 3 Wörter suchst, lassen wir alles ab 2 Treffern durch
                return matchCount >= 2;
            }).ToList();
        }
    }
}
        