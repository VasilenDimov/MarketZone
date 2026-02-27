using MarketZone.Common;
using MarketZone.Data;
using MarketZone.Data.Enums;
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
		private readonly ICategoryHierarchyService categoryHierarchyService;

		public UserService(
			ApplicationDbContext context,
			IReviewService reviewService,
			ICategoryHierarchyService categoryHierarchyService)
		{
			this.context = context;
			this.reviewService = reviewService;
			this.categoryHierarchyService = categoryHierarchyService;
		}

		public async Task<UserProfileViewModel> GetProfileAsync(
			string userId,
			string? search,
			string? address,
			int? categoryId,
			decimal? minPrice,
			decimal? maxPrice,
			string? tags,
			string? sort,
			int page,
			string? viewerId)
		{
			const int PageSize = 21;

			var user = await context.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Id == userId);

			if (user == null)
				throw new ArgumentException("User not found");

			var adsQuery = context.Ads
				.AsNoTracking()
				.Where(a => a.UserId == userId)
				.AsQueryable();

			var isOwner = !string.IsNullOrWhiteSpace(viewerId) && viewerId == userId;
			if (!isOwner)
			{
				adsQuery = adsQuery.Where(a => a.Status == AdStatus.Approved);
			}

			if (!string.IsNullOrWhiteSpace(search))
			{
				adsQuery = adsQuery.Where(a =>
					a.Title.Contains(search) ||
					a.Description.Contains(search));
			}

			if (!string.IsNullOrWhiteSpace(address))
			{
				var tokens = address
					.ToLower()
					.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
					.Distinct()
					.ToList();

				foreach (var t in tokens)
				{
					var token = t;
					adsQuery = adsQuery.Where(a => a.Address.ToLower().Contains(token));
				}
			}

			if (categoryId.HasValue)
			{
				var allowedCategoryIds = await categoryHierarchyService
					.GetDescendantCategoryIdsAsync(categoryId.Value);

				adsQuery = adsQuery.Where(a => allowedCategoryIds.Contains(a.CategoryId));
			}

			if (minPrice.HasValue)
				adsQuery = adsQuery.Where(a => a.Price >= minPrice.Value);

			if (maxPrice.HasValue)
				adsQuery = adsQuery.Where(a => a.Price <= maxPrice.Value);

			if (!string.IsNullOrWhiteSpace(tags))
			{
				var tagList = tags
					.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(t => t.Trim().ToLower())
					.Where(t => !string.IsNullOrWhiteSpace(t))
					.Distinct()
					.Take(10)
					.ToList();

				if (tagList.Count > 0)
				{
					adsQuery = adsQuery.Where(a =>
						a.AdTags.Any(at => tagList.Contains(at.Tag.Name.ToLower())));
				}
			}

			adsQuery = sort switch
			{
				"price_asc" => adsQuery.OrderBy(a => a.Price),
				"price_desc" => adsQuery.OrderByDescending(a => a.Price),
				"oldest" => adsQuery.OrderBy(a => a.CreatedOn),
				_ => adsQuery.OrderByDescending(a => a.CreatedOn)
			};

			var totalAds = await adsQuery.CountAsync();

			var ads = await adsQuery
				.Skip((page - 1) * PageSize)
				.Take(PageSize)
				.Select(a => new AdListItemViewModel
				{
					Id = a.Id,
					Title = a.Title,
					Price = a.Price,
					Currency = Currency.EUR,
					CreatedOn = a.CreatedOn,
					MainImageUrl = a.Images
						.OrderBy(i => i.Id)
						.Select(i => i.ImageUrl)
						.FirstOrDefault()
				})
				.ToListAsync();

			var reviews = await reviewService.GetReviewsForUserAsync(userId);

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

			bool canReview = false;
			if (!string.IsNullOrWhiteSpace(viewerId))
				canReview = await reviewService.CanReviewAsync(viewerId, userId);

			string? categoryName = null;
			if (categoryId.HasValue)
			{
				categoryName = await context.Categories
					.AsNoTracking()
					.Where(c => c.Id == categoryId.Value)
					.Select(c => c.Name)
					.FirstOrDefaultAsync();
			}

			var displayName = user.IsDeleted ? "Deleted user" : user.UserName!;
			var profilePic = string.IsNullOrWhiteSpace(user.ProfilePictureUrl)
				? AppConstants.DefaultAvatarUrl
				: user.ProfilePictureUrl;

			return new UserProfileViewModel
			{
				UserId = user.Id,
				UserName = displayName,
				Email = user.IsDeleted ? string.Empty : (user.Email ?? string.Empty),
				ProfilePictureUrl = profilePic,
				CreatedOn = user.CreatedOn,
				LastOnlineOn = user.LastOnlineOn,

				SearchTerm = search,
				Address = address,
				CategoryId = categoryId,
				CategoryName = categoryName,
				MinPrice = minPrice,
				MaxPrice = maxPrice,
				Tags = tags,
				Sort = sort,

				CurrentPage = page,
				TotalPages = (int)Math.Ceiling(totalAds / (double)PageSize),

				Ads = ads,
				Reviews = reviews,
				AverageRating = reviewStats?.AverageRating ?? 0,
				ReviewCount = reviewStats?.ReviewCount ?? 0,
				CanReview = canReview
			};
		}
	}
}