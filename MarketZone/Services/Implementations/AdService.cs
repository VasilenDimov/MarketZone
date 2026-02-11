using MarketZone.Data;
using MarketZone.Data.Models;
using MarketZone.Services.Interfaces;
using MarketZone.ViewModels.Ad;
using Microsoft.EntityFrameworkCore;

namespace MarketZone.Services.Implementations
{
	public class AdService : IAdService
	{
		private readonly ApplicationDbContext context;
		private readonly IImageService imageService;

		public AdService(ApplicationDbContext context, IImageService imageService)
		{
			this.context = context;
			this.imageService = imageService;
		}

		public async Task<int> CreateAsync(AdCreateModel model, string userId)
		{
			if (model.Images == null || model.Images.Count == 0)
			{
				throw new InvalidOperationException("At least one image is required.");
			}

			// Validate that the category exists
			var categoryExists = await context.Categories
				.AnyAsync(c => c.Id == model.CategoryId);

			if (!categoryExists)
			{
				throw new InvalidOperationException("The selected category does not exist.");
			}

			var ad = new Ad
			{
				Title = model.Title,
				Description = model.Description,
				Price = model.Price,
				Currency = model.Currency,
				Address = model.Address,
				Latitude = model.Latitude,
				Longitude = model.Longitude,
				CategoryId = model.CategoryId!.Value,
				Condition = model.Condition,
				UserId = userId,
				CreatedOn = DateTime.UtcNow
			};

			//Images
			foreach (var image in model.Images)
			{
				var imageUrl = await imageService.UploadAdImageAsync(image);

				ad.Images.Add(new AdImage
				{
					ImageUrl = imageUrl
				});
			}

			//Tags (optional, max 20)
			if (!string.IsNullOrWhiteSpace(model.Tags))
			{
				var tagNames = model.Tags
					.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(t => t.Trim().ToLower())
					.Distinct()
					.Take(20);

				foreach (var tagName in tagNames)
				{
					var tag = await context.Tags
						.FirstOrDefaultAsync(t => t.Name == tagName)
						?? new Tag { Name = tagName };

					ad.AdTags.Add(new AdTag
					{
						Tag = tag
					});
				}
			}

			await context.Ads.AddAsync(ad);
			await context.SaveChangesAsync();

			return ad.Id;
		}

		public async Task<AdDetailsModel?> GetDetailsAsync(int id, string? userId)
		{
			return await context.Ads
				.AsNoTracking()
				.Where(a => a.Id == id)
				.Select(a => new AdDetailsModel
				{
					Id = a.Id,
					Title = a.Title,
					Description = a.Description,
					Price = a.Price,
					Currency = a.Currency,
					Condition = a.Condition,
					Address = a.Address,
					Latitude = a.Latitude,
					Longitude = a.Longitude,
					CreatedOn = a.CreatedOn,
					SellerId = a.UserId,

					SellerName = a.User.UserName!,
					CategoryPath = a.Category.ParentCategory != null
						? a.Category.ParentCategory.Name + " → " + a.Category.Name
						: a.Category.Name,

					ImageUrls = a.Images
						.OrderBy(i => i.Id)
						.Select(i => i.ImageUrl)
						.ToList(),

					IsFavorite = userId != null &&
						context.Favorites.Any(f =>
							f.AdId == a.Id && f.UserId == userId)
				})
				.FirstOrDefaultAsync();
		}

		public async Task<IEnumerable<AdListItemViewModel>> GetMyAdsAsync(string userId)
		{
			return await context.Ads
				.AsNoTracking()
				.Where(a => a.UserId == userId)
				.OrderByDescending(a => a.CreatedOn)
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
		                  .FirstOrDefault(),
					CanEdit = true
				})
				.ToListAsync();
		}
		public async Task<AdCreateModel?> GetEditModelAsync(int adId, string userId)
		{
			var ad = await context.Ads
				.AsNoTracking()
				.Include(a => a.Images)
				.Include(a => a.AdTags)
				.ThenInclude(at => at.Tag)
				.FirstOrDefaultAsync(a => a.Id == adId && a.UserId == userId);

			if (ad == null)
				return null;

			return new AdCreateModel
			{
				Id = ad.Id,
				Title = ad.Title,
				Description = ad.Description,
				Price = ad.Price,
				Currency = ad.Currency,
				Condition = ad.Condition,
				Address = ad.Address,
				Latitude = ad.Latitude,
				Longitude = ad.Longitude,
				CategoryId = ad.CategoryId,
				Tags = string.Join(", ", ad.AdTags.Select(t => t.Tag.Name)),
				ExistingImageUrls = ad.Images
	             .OrderBy(i => i.Id)
	             .Select(i => i.ImageUrl)
                 .ToList()
			};

		}
		public async Task<bool> UpdateAsync(AdCreateModel model, string userId)
		{
			var ad = await context.Ads
				.Include(a => a.Images)
				.Include(a => a.AdTags)
					.ThenInclude(at => at.Tag)
				.FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == userId);

			if (ad == null)
				return false;

			// Update scalar fields
			ad.Title = model.Title;
			ad.Description = model.Description;
			ad.Price = model.Price;
			ad.Currency = model.Currency;
			ad.Condition = model.Condition;
			ad.Address = model.Address;
			ad.Latitude = model.Latitude;
			ad.Longitude = model.Longitude;
			ad.CategoryId = model.CategoryId!.Value;

			// IMAGES

			// Remove images that user removed in UI
			var imagesToRemove = ad.Images
				.Where(i => !model.ExistingImageUrls.Contains(i.ImageUrl))
				.ToList();

			foreach (var img in imagesToRemove)
			{
				ad.Images.Remove(img);

				await imageService.DeleteImageAsync(img.ImageUrl);
			}

			// Add newly uploaded images
			foreach (var image in model.Images)
			{
				var imageUrl = await imageService.UploadAdImageAsync(image);
				ad.Images.Add(new AdImage { ImageUrl = imageUrl });
			}

			// Enforce at least one image AFTER merge
			if (!ad.Images.Any())
				throw new InvalidOperationException("At least one image is required.");

			// TAGS

			ad.AdTags.Clear();

			if (!string.IsNullOrWhiteSpace(model.Tags))
			{
				var tags = model.Tags
					.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(t => t.Trim().ToLower())
					.Distinct();

				foreach (var tagName in tags)
				{
					var tag = await context.Tags
						.FirstOrDefaultAsync(t => t.Name == tagName)
						?? new Tag { Name = tagName };

					ad.AdTags.Add(new AdTag { Tag = tag });
				}
			}

			await context.SaveChangesAsync();
			return true;
		}
		public async Task<bool> DeleteAsync(int adId, string userId)
		{
			var ad = await context.Ads
				.Include(a => a.Images)
				.FirstOrDefaultAsync(a => a.Id == adId && a.UserId == userId);

			if (ad == null)
				return false;

			// Delete physical images
			foreach (var img in ad.Images.ToList())
			{
				ad.Images.Remove(img);
				await imageService.DeleteImageAsync(img.ImageUrl);
			}

			context.Ads.Remove(ad);
			await context.SaveChangesAsync();

			return true;
		}
		public async Task<AdSearchViewModel> SearchAsync(string? search, int page, string? userId)
		{
			const int PageSize = 21;

			var query = context.Ads
				.AsNoTracking();

			//Exclude user's own ads if logged in
			if (!string.IsNullOrEmpty(userId))
			{
				query = query.Where(a => a.UserId != userId);
			}

			//Search by title
			if (!string.IsNullOrWhiteSpace(search))
			{
				query = query.Where(a =>
					a.Title.Contains(search));
			}

			var totalAds = await query.CountAsync();

			var ads = await query
				.OrderByDescending(a => a.CreatedOn)
				.Skip((page - 1) * PageSize)
				.Take(PageSize)
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

			return new AdSearchViewModel
			{
				SearchTerm = search,
				CurrentPage = page,
				TotalPages = (int)Math.Ceiling(totalAds / (double)PageSize),
				Ads = ads
			};
		}

	}
}
