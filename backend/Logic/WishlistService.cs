using backend.Data;

namespace backend.Logic;

public class WishlistService
{
    private readonly WishlistRepository _repo;

    public WishlistService(WishlistRepository repo)
    {
        _repo = repo;
    }

    public Task ToggleButton(int userId, int productId)
    {
        return _repo.Add(userId, productId); 
    }


    public Task Remove(int userId, int productId)
    {
        return _repo.Remove(userId, productId);
    }

    public Task<List<int>> Get(int userId)
    {
        return _repo.GetWishlistProductIds(userId);
    }
}