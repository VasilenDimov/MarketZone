using MarketZone.Data;
using MarketZone.Services.Interfaces;
using MarketZone.ViewModels.Ad;
using MarketZone.ViewModels.User;
using Microsoft.EntityFrameworkCore;

namespace MarketZone.Services.Implementations
{
	public class UserService : IUserService
	{
		private readonly ApplicationDbContext context;

		public UserService(ApplicationDbContext context)
		{
			this.context = context;
		}

		public async Task<UserProfileViewModel> GetProfileAsync(
			string userId,
			string? search,
			string? sort)
		{
			var user = await context.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Id == userId);

			if (user == null)
			{
				throw new ArgumentException("User not found");
			}

			var adsQuery = context.Ads
				.AsNoTracking()
				.Where(a => a.UserId == userId);

			// Search
			if (!string.IsNullOrWhiteSpace(search))
			{
				adsQuery = adsQuery.Where(a =>
					a.Title.Contains(search) ||
					a.Description.Contains(search));
			}

			// Sorting
			adsQuery = sort switch
			{
				"price_asc" => adsQuery.OrderBy(a => a.Price),
				"price_desc" => adsQuery.OrderByDescending(a => a.Price),
				"oldest" => adsQuery.OrderBy(a => a.CreatedOn),
				_ => adsQuery.OrderByDescending(a => a.CreatedOn)
			};

			var ads = await adsQuery
				.Select(a => new AdListItemViewModel
				{
					Id = a.Id,
					Title = a.Title,
					Price = a.Price,
					Currency = a.Currency,
					CreatedOn = a.CreatedOn,
					MainImageUrl = a.Images
						.OrderBy(i => i.Id)
						.Select(i => i.ImageUrl)
						.FirstOrDefault()
				})
				.ToListAsync();

			return new UserProfileViewModel
			{
				UserId = user.Id,
				UserName = user.UserName!,
				Email = user.Email!,
				ProfilePictureUrl = user.ProfilePictureUrl,

				CreatedOn = user.CreatedOn,
				LastOnlineOn = user.LastOnlineOn,

				SearchTerm = search,
				Sort = sort,
				Ads = ads
			};
		}
	}
}
