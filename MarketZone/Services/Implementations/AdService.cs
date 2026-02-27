using MarketZone.Common;
using MarketZone.Data;
using MarketZone.Data.Enums;
using MarketZone.Data.Models;
using MarketZone.Services.Interfaces;
using MarketZone.ViewModels.Ad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarketZone.Services.Implementations
{
	public class AdService : IAdService
	{
		private readonly ApplicationDbContext context;
		private readonly IImageService imageService;
		private readonly ICategoryHierarchyService categoryHierarchyService;
		private readonly UserManager<User> userManager;

		public AdService(
			ApplicationDbContext context,
			IImageService imageService,
			ICategoryHierarchyService categoryHierarchyService,
			UserManager<User> userManager)
		{
			this.context = context;
			this.imageService = imageService;
			this.categoryHierarchyService = categoryHierarchyService;
			this.userManager = userManager;
		}

		public async Task<int> CreateAsync(AdCreateModel model, string userId)
		{
			if (model.Images == null || model.Images.Count == 0)
				throw new InvalidOperationException("At least one image is required.");

			var categoryExists = await context.Categories.AnyAsync(c => c.Id == model.CategoryId);
			if (!categoryExists)
				throw new InvalidOperationException("The selected category does not exist.");

			// Determine role-based status
			var user = await userManager.FindByIdAsync(userId);
			var isStaff = user != null &&
						  (await userManager.IsInRoleAsync(user, "Admin") ||
						   await userManager.IsInRoleAsync(user, "Moderator"));

			var initialStatus = isStaff ? AdStatus.Approved : AdStatus.Pending;

			var ad = new Ad
			{
				Title = model.Title,
				Description = model.Description,
				Price = model.Price,
				Currency = Currency.EUR,
				Address = model.Address,
				Latitude = model.Latitude,
				Longitude = model.Longitude,
				CategoryId = model.CategoryId!.Value,
				Condition = model.Condition,
				UserId = userId,
				CreatedOn = DateTime.UtcNow,

				Status = initialStatus,
				ReviewedOn = isStaff ? DateTime.UtcNow : null,
				ReviewedByUserId = isStaff ? userId : null,
				RejectionReason = null
			};

			foreach (var image in model.Images)
			{
				var imageUrl = await imageService.UploadAdImageAsync(image);
				ad.Images.Add(new AdImage { ImageUrl = imageUrl });
			}

			if (!string.IsNullOrWhiteSpace(model.Tags))
			{
				var tagNames = model.Tags
					.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(t => t.Trim().ToLower())
					.Distinct()
					.Take(20);

				foreach (var tagName in tagNames)
				{
					var tag = await context.Tags.FirstOrDefaultAsync(t => t.Name == tagName)
						?? new Tag { Name = tagName };

					ad.AdTags.Add(new AdTag { Tag = tag });
				}
			}

			await context.Ads.AddAsync(ad);
			await context.SaveChangesAsync();

			return ad.Id;
		}

		public async Task<AdDetailsModel?> GetDetailsAsync(int id, string? userId, bool isModeratorOrAdmin)
		{
			var data = await context.Ads
				.AsNoTracking()
				.Where(a => a.Id == id)
				.Select(a => new
				{
					a.Status,
					a.UserId,
					CategoryId = a.CategoryId,
					Model = new AdDetailsModel
					{
						Id = a.Id,
						Title = a.Title,
						Description = a.Description,
						Price = a.Price,
						Currency = Currency.EUR,
						Condition = a.Condition,
						Address = a.Address,
						Latitude = a.Latitude,
						Longitude = a.Longitude,
						CreatedOn = a.CreatedOn,
						Status = a.Status,
						SellerId = a.UserId,

						SellerName = a.User.IsDeleted ? "Deleted user" : a.User.UserName!,
						SellerProfilePictureUrl =
							string.IsNullOrWhiteSpace(a.User.ProfilePictureUrl)
								? AppConstants.DefaultAvatarUrl
								: a.User.ProfilePictureUrl,

						CategoryPath = string.Empty,

						ImageUrls = a.Images
							.OrderBy(i => i.Id)
							.Select(i => i.ImageUrl)
							.ToList(),

						IsFavorite = userId != null &&
							context.Favorites.Any(f => f.AdId == a.Id && f.UserId == userId)
					}
				})
				.FirstOrDefaultAsync();

			if (data == null)
				return null;

			if (data.Status != AdStatus.Approved)
			{
				var isOwner = userId != null && data.UserId == userId;
				if (!isOwner && !isModeratorOrAdmin)
					return null;
			}

			data.Model.CategoryPath = await BuildCategoryPathAsync(data.CategoryId);
			return data.Model;
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
					Currency = Currency.EUR,
					CreatedOn = a.CreatedOn,
					Status = a.Status,
					RejectionReason = a.RejectionReason,
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
				Currency = Currency.EUR,
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

		public async Task<bool> UpdateAsync(AdCreateModel model, string userId, bool autoApprove)
		{
			var ad = await context.Ads
				.Include(a => a.Images)
				.Include(a => a.AdTags)
					.ThenInclude(at => at.Tag)
				.FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == userId);

			if (ad == null)
				return false;

			ad.Title = model.Title;
			ad.Description = model.Description;
			ad.Price = model.Price;
			ad.Currency = Currency.EUR;
			ad.Condition = model.Condition;
			ad.Address = model.Address;
			ad.Latitude = model.Latitude;
			ad.Longitude = model.Longitude;
			ad.CategoryId = model.CategoryId!.Value;

			ad.Status = autoApprove ? AdStatus.Approved : AdStatus.Pending;

			if (!autoApprove)
			{
				ad.ReviewedOn = null;
				ad.ReviewedByUserId = null;
				ad.RejectionReason = null;
			}

			var keepUrls = new HashSet<string>(
				model.ExistingImageUrls ?? Enumerable.Empty<string>(),
				StringComparer.OrdinalIgnoreCase);

			var imagesToRemove = ad.Images
				.Where(i => !keepUrls.Contains(i.ImageUrl))
				.ToList();

			foreach (var img in imagesToRemove)
			{
				ad.Images.Remove(img);
				await imageService.DeleteImageAsync(img.ImageUrl);
			}

			foreach (var image in model.Images)
			{
				var imageUrl = await imageService.UploadAdImageAsync(image);
				ad.Images.Add(new AdImage { ImageUrl = imageUrl });
			}

			if (!ad.Images.Any())
				throw new InvalidOperationException("At least one image is required.");

			ad.AdTags.Clear();

			if (!string.IsNullOrWhiteSpace(model.Tags))
			{
				var tags = model.Tags
					.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(t => t.Trim().ToLower())
					.Distinct()
					.Take(20);

				foreach (var tagName in tags)
				{
					var tag = await context.Tags.FirstOrDefaultAsync(t => t.Name == tagName)
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

			foreach (var img in ad.Images.ToList())
			{
				ad.Images.Remove(img);
				await imageService.DeleteImageAsync(img.ImageUrl);
			}

			context.Ads.Remove(ad);
			await context.SaveChangesAsync();

			return true;
		}

		public async Task<AdSearchViewModel> SearchAsync(
			string? search,
			string? address,
			int? categoryId,
			decimal? minPrice,
			decimal? maxPrice,
			string? tags,
			string? sort,
			int page,
			string? userId)
		{
			const int PageSize = 21;

			var query = context.Ads.AsNoTracking().AsQueryable();
			query = query.Where(a => a.Status == AdStatus.Approved);

			if (!string.IsNullOrEmpty(userId))
				query = query.Where(a => a.UserId != userId);

			if (!string.IsNullOrWhiteSpace(search))
				query = query.Where(a => a.Title.Contains(search));

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
					query = query.Where(a => a.Address.ToLower().Contains(token));
				}
			}

			if (categoryId.HasValue)
			{
				var allowedCategoryIds = await categoryHierarchyService
					.GetDescendantCategoryIdsAsync(categoryId.Value);

				query = query.Where(a => allowedCategoryIds.Contains(a.CategoryId));
			}

			if (minPrice.HasValue)
				query = query.Where(a => a.Price >= minPrice.Value);

			if (maxPrice.HasValue)
				query = query.Where(a => a.Price <= maxPrice.Value);

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
					query = query.Where(a => a.AdTags.Any(at => tagList.Contains(at.Tag.Name.ToLower())));
				}
			}

			query = sort switch
			{
				"oldest" => query.OrderBy(a => a.CreatedOn),
				"price_asc" => query.OrderBy(a => a.Price),
				"price_desc" => query.OrderByDescending(a => a.Price),
				_ => query.OrderByDescending(a => a.CreatedOn)
			};

			var totalAds = await query.CountAsync();

			var ads = await query
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

			string? categoryName = null;
			if (categoryId.HasValue)
			{
				categoryName = await context.Categories
					.AsNoTracking()
					.Where(c => c.Id == categoryId.Value)
					.Select(c => c.Name)
					.FirstOrDefaultAsync();
			}

			return new AdSearchViewModel
			{
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
				Ads = ads
			};
		}

		private async Task<string> BuildCategoryPathAsync(int categoryId)
		{
			var names = new List<string>();
			int? currentId = categoryId;

			while (currentId != null)
			{
				var cat = await context.Categories
					.AsNoTracking()
					.Where(c => c.Id == currentId.Value)
					.Select(c => new { c.Name, c.ParentCategoryId })
					.FirstOrDefaultAsync();

				if (cat == null)
					break;

				names.Add(cat.Name);
				currentId = cat.ParentCategoryId;
			}

			names.Reverse();
			return string.Join(" → ", names);
		}
		public async Task<AdSearchViewModel> GetPendingAsync(string? search,
	    string? address,int? categoryId,decimal? minPrice,decimal? maxPrice,
	    string? tags,string? sort,int page)
		{
			const int PageSize = 21;

			var query = context.Ads
				.AsNoTracking()
				.AsQueryable();

			query = query.Where(a => a.Status == AdStatus.Pending);

			if (!string.IsNullOrWhiteSpace(search))
				query = query.Where(a => a.Title.Contains(search));

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
					query = query.Where(a => a.Address.ToLower().Contains(token));
				}
			}

			// Category (include descendants)
			string? categoryName = null;
			if (categoryId.HasValue)
			{
				var allowedCategoryIds = await categoryHierarchyService
					.GetDescendantCategoryIdsAsync(categoryId.Value);

				query = query.Where(a => allowedCategoryIds.Contains(a.CategoryId));

				categoryName = await context.Categories
					.AsNoTracking()
					.Where(c => c.Id == categoryId.Value)
					.Select(c => c.Name)
					.FirstOrDefaultAsync();
			}

			// Price
			if (minPrice.HasValue)
				query = query.Where(a => a.Price >= minPrice.Value);

			if (maxPrice.HasValue)
				query = query.Where(a => a.Price <= maxPrice.Value);

			// Tags
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
					query = query.Where(a =>
						a.AdTags.Any(at => tagList.Contains(at.Tag.Name.ToLower())));
				}
			}

			// Sorting
			query = sort switch
			{
				"oldest" => query.OrderBy(a => a.CreatedOn),
				"price_asc" => query.OrderBy(a => a.Price),
				"price_desc" => query.OrderByDescending(a => a.Price),
				_ => query.OrderByDescending(a => a.CreatedOn)
			};

			var total = await query.CountAsync();

			var ads = await query
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

			return new AdSearchViewModel
			{
				SearchTerm = search,
				Address = address,
				CategoryId = categoryId,
				CategoryName = categoryName,
				MinPrice = minPrice,
				MaxPrice = maxPrice,
				Tags = tags,
				Sort = sort,

				CurrentPage = page,
				TotalPages = (int)Math.Ceiling(total / (double)PageSize),
				Ads = ads
			};
		}

		public async Task<bool> ApproveAsync(int adId, string reviewerId)
		{
			var ad = await context.Ads
				.FirstOrDefaultAsync(a => a.Id == adId);

			if (ad == null)
				return false;

			// Only pending ads can be approved
			if (ad.Status != AdStatus.Pending)
				return false;

			ad.Status = AdStatus.Approved;
			ad.ReviewedOn = DateTime.UtcNow;
			ad.ReviewedByUserId = reviewerId;
			ad.RejectionReason = null;

			await context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> RejectAsync(int adId, string reviewerId, string reason)
		{
			var ad = await context.Ads
				.FirstOrDefaultAsync(a => a.Id == adId);

			if (ad == null)
				return false;

			// Only pending ads can be rejected
			if (ad.Status != AdStatus.Pending)
				return false;

			ad.Status = AdStatus.Rejected;
			ad.ReviewedOn = DateTime.UtcNow;
			ad.ReviewedByUserId = reviewerId;
			ad.RejectionReason = string.IsNullOrWhiteSpace(reason)
				? "Rejected by moderator."
				: reason.Trim();

			await context.SaveChangesAsync();
			return true;
		}
	}
}