using MarketZone.ViewModels.AdminUsers;
using MarketZone.ViewModels.User;

namespace MarketZone.Services.Interfaces
{
	public interface IUserService
	{
		Task<UserProfileViewModel> GetProfileAsync(string userId, string? search,
		string? address,int? categoryId, decimal? minPrice, decimal? maxPrice,
		string? tags,string? sort, int page, string? viewerId);
		Task<bool> SoftDeleteUserAsync(string targetUserId, string adminId);
		Task<AdminUsersPageViewModel> GetAdminUsersPageAsync(string? search,
		int page, string adminId);
		Task<bool> PromoteToModeratorAsync(string targetUserId, string adminId);
		Task<bool> DemoteFromModeratorAsync(string targetUserId, string adminId);

	}
}
