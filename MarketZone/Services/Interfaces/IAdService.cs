using MarketZone.ViewModels.Ad;

namespace MarketZone.Services.Interfaces
{
	public interface IAdService
	{
		Task<int> CreateAsync(AdCreateModel model, string userId);
		Task<AdDetailsModel?> GetDetailsAsync(int id, string? userId);
		Task<IEnumerable<AdListItemViewModel>> GetMyAdsAsync(string userId);
		Task<AdCreateModel?> GetEditModelAsync(int adId, string userId);
		Task<bool> UpdateAsync(AdCreateModel model, string userId);	
        Task<bool> DeleteAsync(int adId, string userId);
		Task<AdSearchViewModel> SearchAsync(string? search,string? address,
	    int? categoryId,decimal? minPrice,decimal? maxPrice,string? tags,
	    string? sort,int page,string? userId);

	}
}
