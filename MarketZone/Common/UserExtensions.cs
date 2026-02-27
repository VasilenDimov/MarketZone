using MarketZone.Data.Models;

namespace MarketZone.Common
{
	public static class UserExtensions
	{
		public static string GetDisplayName(this User user)
		{
			return user.IsDeleted ? "Deleted user" : user.UserName!;
		}
	}
}
