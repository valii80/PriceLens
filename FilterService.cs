using PriceLens;
using System.Collections.Generic;
using System.Linq;

namespace PriceLens
{
    public class FilterService
    {
        // Filter für Angebote
        public List<Angebot> Filter(List<Angebot> angebote, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return angebote;

            var keywords = query
                .ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var ranked = angebote
                .Select(a =>
                {
                    var name = (a.produkt?.name ?? "").ToLower();

                    int score = 0;

                    foreach (var k in keywords)
                    {
                        if (name.Contains(k))
                            score++;
                    }

                    return new
                    {
                        Angebot = a,
                        Score = score
                    };
                })
                // 🔥 wichtigste Änderung
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Angebot.preis)
                .Select(x => x.Angebot)
                .ToList();

            return ranked;
        }
    }
}