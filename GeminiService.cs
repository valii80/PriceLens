using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PriceLens;

public class GeminiService
{
    private readonly HttpClient http = new();

    // ⚠️ API KEY GEMINI (vor GitHub Push entfernen!)
    private readonly string apiKey = "";

    public async Task<string> GenerateComparison(string prompt)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent?key={apiKey}";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        int maxRetries = 3;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await http.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                // ❌ API Fehler (z.B. 503)
                if (!response.IsSuccessStatusCode)
                {
                    if ((int)response.StatusCode == 503 && attempt < maxRetries)
                    {
                        await Task.Delay(2000); // warten & nochmal versuchen
                        continue;
                    }

                    return $"⚠️ API Fehler: {response.StatusCode}\n{responseJson}";
                }

                using var doc = JsonDocument.Parse(responseJson);

                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text ?? "Keine Antwort";
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries)
                    return $"❌ Fehler: {ex.Message}";

                await Task.Delay(2000);
            }
        }

        return "⚠️ KI-Server aktuell überlastet. Bitte später erneut versuchen.";
    }
}