using MarketZone.ViewModels.Ad;

namespace MarketZone.Services.Interfaces
{
	public interface IAdService
	{
		Task<int> CreateAsync(AdCreateModel model, string userId);
		Task<AdDetailsModel?> GetDetailsAsync(int id, string? userId,bool isModeratorOrAdmin);
		Task<IEnumerable<AdListItemViewModel>> GetMyAdsAsync(string userId);
		Task<AdCreateModel?> GetEditModelAsync(int adId, string userId);
		Task<bool> UpdateAsync(AdCreateModel model, string userId, bool autoApprove);
		Task<bool> DeleteAsync(int adId, string userId);
		Task<AdSearchViewModel> SearchAsync(string? search,string? address,
	    int? categoryId,decimal? minPrice,decimal? maxPrice,string? tags,
	    string? sort,int page,string? userId);
		Task<AdSearchViewModel> GetPendingAsync(string? search,string? address,
		int? categoryId,decimal? minPrice,decimal? maxPrice,string? tags,
		string? sort,int page);
		Task<bool> ApproveAsync(int adId, string reviewerId);
		Task<bool> RejectAsync(int adId, string reviewerId, string reason);

	}
}
