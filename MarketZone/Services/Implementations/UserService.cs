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
		private readonly IReviewService reviewService;

		public UserService(
			ApplicationDbContext context,
			IReviewService reviewService)
		{
			this.context = context;
			this.reviewService = reviewService;
		}

		public async Task<UserProfileViewModel> GetProfileAsync(
			string userId,
			string? search,
			string? sort,
			string? viewerId)
		{
			// USER
			var user = await context.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Id == userId);

			if (user == null)
				throw new ArgumentException("User not found");

			// ADS
			var adsQuery = context.Ads
				.AsNoTracking()
				.Where(a => a.UserId == userId);

			if (!string.IsNullOrWhiteSpace(search))
			{
				adsQuery = adsQuery.Where(a =>
					a.Title.Contains(search) ||
					a.Description.Contains(search));
			}

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

			// REVIEWS (list)
			var reviews = await reviewService.GetReviewsForUserAsync(userId);

			// REVIEW STATS (ONE QUERY, SAFE)
			var reviewStats = await context.Reviews
				.AsNoTracking()
				.Where(r => r.ReviewedUserId == userId)
				.GroupBy(r => r.ReviewedUserId)
				.Select(g => new
				{
					AverageRating = g.Average(r => (double?)r.Rating) ?? 0,
					ReviewCount = g.Count()
				})
				.FirstOrDefaultAsync();

			// CAN REVIEW
			bool canReview = false;
			if (!string.IsNullOrWhiteSpace(viewerId))
			{
				canReview = await reviewService.CanReviewAsync(viewerId, userId);
			}

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

				Ads = ads,
				Reviews = reviews,
				AverageRating = reviewStats?.AverageRating ?? 0,
				ReviewCount = reviewStats?.ReviewCount ?? 0,
				CanReview = canReview
			};
		}
	}
}
