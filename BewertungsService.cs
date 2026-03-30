using System.Linq;
using System.Collections.Generic;

namespace PriceLens;

public class BewertungsService
{
    public List<Angebot> Rank(List<Angebot> angebote, string query)
    {
        var queryWords = query
            .ToLower()
            .Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var a in angebote)
        {
            var name = (a.produkt?.name ?? "").ToLower();

            var queryLower = query.ToLower();

            // 🔹 NUR die ersten 3–5 Wörter sind wichtig
            var nameWords = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var coreWords = nameWords.Take(5).ToList();

            int score = 0;

            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // 🔹 1. Query Match
            int matches = queryWords.Count(q => name.Contains(q));
            score += matches * 50;

            // 🔹 2. Exakte Phrase
            if (name.Contains(queryLower))
                score += 100;

            // 🔹 3. Start Bonus
            if (name.StartsWith(queryLower))
                score += 80;

            // 🔥 4. PENALTY SYSTEM (DER GAME CHANGER)

            // 👉 sehr lange Titel = meistens Zubehör / Spam
            if (words.Length > queryWords.Length + 4)
                score -= 150;

            // 👉 viele Zahlen / Modelle gemischt = Zubehör typisch
            int digitCount = words.Count(w => w.Any(char.IsDigit));
            if (digitCount > 3)
                score -= 100;

            // 👉 wenn viele unterschiedliche Modelle drin (14 15 16 17)
            int modelSpam = words.Count(w => w.Length <= 3 && w.All(char.IsDigit));
            if (modelSpam >= 3)
                score -= 200;

            // 👉 Titel enthält zu viele generische Wörter
            if (words.Length > 10)
                score -= 100;

            // 🔹 5. Gute Struktur Bonus
            if (words.Length <= queryWords.Length + 3)
                score += 50;

            a.score = score;
        }

        return angebote
            .OrderByDescending(a => a.score)
            .ToList();
    }
}