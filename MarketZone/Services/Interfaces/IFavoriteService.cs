using MarketZone.ViewModels.Ad;

namespace MarketZone.Services.Interfaces
{
	public interface IFavoriteService
	{
		Task<bool> ToggleAsync(int adId, string userId);
		Task<bool> IsFavoriteAsync(int adId, string userId);
		Task<IEnumerable<AdListItemViewModel>> GetFavoritesAsync(string userId);
	}

}
