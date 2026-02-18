using MarketZone.ViewModels.User;

namespace MarketZone.Services.Interfaces
{
	public interface IUserService
	{
		Task<UserProfileViewModel> GetProfileAsync(string userId, string? search,
		string? address,int? categoryId, decimal? minPrice, decimal? maxPrice,
		string? tags,string? sort, int page, string? viewerId);

	}
}
