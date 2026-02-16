using MarketZone.Data;
using MarketZone.Data.Models;
using MarketZone.Helpers;
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
		private readonly ICategoryService categoryService;

		public UserService(
			ApplicationDbContext context,
			IReviewService reviewService,
			ICategoryService categoryService)
		{
			this.context = context;
			this.reviewService = reviewService;
			this.categoryService = categoryService;
		}

		public async Task<UserProfileViewModel> GetProfileAsync(
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
			string? viewerId)
		{
			// USER
			var user = await context.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Id == userId);

			if (user == null)
				throw new ArgumentException("User not found");

			// ADS
			IQueryable<Ad> adsQuery = context.Ads
				.AsNoTracking()
				.Include(a => a.Category)
				.Include(a => a.AdTags)
				.ThenInclude(at => at.Tag)
				.Where(a => a.UserId == userId);

			if (!string.IsNullOrWhiteSpace(search))
			{
				adsQuery = adsQuery.Where(a =>
					a.Title.Contains(search) ||
					a.Description.Contains(search));
			}

			// Price range filter
			if (minPrice.HasValue)
			{
				adsQuery = adsQuery.Where(a => a.Price >= minPrice.Value);
			}
			if (maxPrice.HasValue)
			{
				adsQuery = adsQuery.Where(a => a.Price <= maxPrice.Value);
			}

			// Category filter (including subcategories)
			if (categoryId.HasValue && categoryId.Value > 0)
			{
				var categoryIds = await GetCategoryWithDescendants(categoryId.Value);
				adsQuery = adsQuery.Where(a => categoryIds.Contains(a.CategoryId));
			}

			// Tags filter
			if (!string.IsNullOrWhiteSpace(tags))
			{
				var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(t => t.Trim().ToLower())
					.Take(10)
					.ToList();

				if (tagList.Any())
				{
					adsQuery = adsQuery.Where(a => a.AdTags.Any(at =>
						tagList.Contains(at.Tag.Name.ToLower())));
				}
			}

			// Location filter (distance-based)
			List<AdListItemViewModel> ads;
			if (latitude.HasValue && longitude.HasValue && radiusKm.HasValue)
			{
				var adsList = await adsQuery.ToListAsync();

				adsList = adsList.Where(a =>
					a.Latitude.HasValue &&
					a.Longitude.HasValue &&
					GeoHelper.CalculateDistance(latitude.Value, longitude.Value,
						a.Latitude.Value, a.Longitude.Value) <= radiusKm.Value)
					.ToList();

				// Apply sorting after filtering
				adsList = sort switch
				{
					"price_asc" => adsList.OrderBy(a => a.Price).ToList(),
					"price_desc" => adsList.OrderByDescending(a => a.Price).ToList(),
					"oldest" => adsList.OrderBy(a => a.CreatedOn).ToList(),
					_ => adsList.OrderByDescending(a => a.CreatedOn).ToList()
				};

				ads = adsList
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
					.ToList();
			}
			else
			{
				adsQuery = sort switch
				{
					"price_asc" => adsQuery.OrderBy(a => a.Price),
					"price_desc" => adsQuery.OrderByDescending(a => a.Price),
					"oldest" => adsQuery.OrderBy(a => a.CreatedOn),
					_ => adsQuery.OrderByDescending(a => a.CreatedOn)
				};

				ads = await adsQuery
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
			}

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
				MinPrice = minPrice,
				MaxPrice = maxPrice,
				CategoryId = categoryId,
				Tags = tags,
				Address = address,
				Latitude = latitude,
				Longitude = longitude,
				RadiusKm = radiusKm,

				Ads = ads,
				Reviews = reviews,
				AverageRating = reviewStats?.AverageRating ?? 0,
				ReviewCount = reviewStats?.ReviewCount ?? 0,
				CanReview = canReview,
				Categories = await categoryService.GetAllAsync()
			};
		}

		// Helper method for category hierarchy
		private async Task<List<int>> GetCategoryWithDescendants(int categoryId)
		{
			var result = new List<int> { categoryId };

			var children = await context.Categories
				.Where(c => c.ParentCategoryId == categoryId)
				.Select(c => c.Id)
				.ToListAsync();

			foreach (var childId in children)
			{
				result.AddRange(await GetCategoryWithDescendants(childId));
			}

			return result;
		}
	}
}
