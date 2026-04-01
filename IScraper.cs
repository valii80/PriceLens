using System.Threading.Tasks;

namespace PriceLens
{
    public interface IScraper<T>
    {
        Task<T> ScrapeAsync(string input);
    }
}