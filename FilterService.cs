using PriceLens;
using System.Collections.Generic;
using System.Linq;

namespace PriceLens
{
    public class FilterService
    {
        public List<Angebot> Filter(List<Angebot> angebote, string query)
        {
            var queryWords = query
                .ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return angebote
                .Where(a =>
                {
                    var name = (a.produkt?.name ?? "").ToLower();

                    // 🔹 1. MUSS alle Query-Wörter enthalten
                    bool containsAll = queryWords.All(w => name.Contains(w));
                    if (!containsAll)
                        return false;

                    // 🔹 2. PRODUKT-STRUKTUR CHECK (MAGIE HIER)
                    int wordCount = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

                    // 👉 zu viele extra Wörter = Zubehör
                    if (wordCount > queryWords.Length + 10)
                        return false;

                    // 🔹 3. Reihenfolge prüfen
                    int lastIndex = -1;
                    foreach (var word in queryWords)
                    {
                        int index = name.IndexOf(word);
                        if (index < lastIndex)
                            return false;

                        lastIndex = index;
                    }

                    return true;
                })
                .ToList();
        }
    }
}
        