using MarketZone.ViewModels.User;

namespace MarketZone.Services.Interfaces
{
	public interface IUserService
	{
		Task<UserProfileViewModel> GetProfileAsync(
			string userId,
			string? search,
			string? sort,
			string? viewerId);
	}
}
