namespace PriceLens;

public class FavoritService
{
    private readonly List<Angebot> favs = new();

    public void Add(Angebot a) => favs.Add(a);
    public void Remove(Angebot a) => favs.Remove(a);
    public List<Angebot> GetAll() => favs;
}
