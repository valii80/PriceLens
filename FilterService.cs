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

            return angebote
                .Where(a =>
                {
                    var name = (a.produkt ?.name ??"").ToLower();

                    // ✔ mindestens ein Keyword muss vorkommen
                    return keywords.Any(k => name.Contains(k));
                })
                .ToList();
        }
    }
}