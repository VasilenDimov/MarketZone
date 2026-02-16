using MarketZone.ViewModels.User;

namespace MarketZone.Services.Interfaces
{
	public interface IUserService
	{
		Task<UserProfileViewModel> GetProfileAsync(
			string userId,
			string? search,
			string? sort,
			decimal? minPrice,
			decimal? maxPrice,
			int? categoryId,
			string? tags,
			string? address,
			double? latitude,
			double? longitude,
			double? radiusKm,
			string? viewerId);
	}
}
