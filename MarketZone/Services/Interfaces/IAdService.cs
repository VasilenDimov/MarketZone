using MarketZone.ViewModels.Ad;

namespace MarketZone.Services.Interfaces
{
	public interface IAdService
	{
		Task<int> CreateAsync(AdCreateModel model, string userId);
		Task<AdDetailsModel?> GetDetailsAsync(int id);
		Task<IEnumerable<MyAdViewModel>> GetMyAdsAsync(string userId);
		Task<AdCreateModel?> GetEditModelAsync(int adId, string userId);
		Task<bool> UpdateAsync(AdCreateModel model, string userId);


	}
}
