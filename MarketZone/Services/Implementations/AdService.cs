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
		private readonly IWebHostEnvironment env;

		public AdService(ApplicationDbContext context, IWebHostEnvironment env)
		{
			this.context = context;
			this.env = env;
		}

		public async Task<int> CreateAsync(AdCreateModel model, string userId)
		{
			if (model.Images == null || model.Images.Count == 0)
			{
				throw new InvalidOperationException("At least one image is required.");
			}

			var ad = new Ad
			{
				Title = model.Title,
				Description = model.Description,
				Price = model.Price,
				Currency = model.Currency,
				Address = model.Address,
				CategoryId = model.CategoryId,
				Condition = model.Condition,
				UserId = userId,
				CreatedOn = DateTime.UtcNow
			};

			// 🔹 Images
			foreach (var image in model.Images)
			{
				var imageUrl = await SaveImageAsync(image);

				ad.Images.Add(new AdImage
				{
					ImageUrl = imageUrl
				});
			}

			// 🔹 Tags (optional, max 20)
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

		// private helper method to save image and return its URL
		private async Task<string> SaveImageAsync(IFormFile image)
		{
			if (image == null || image.Length == 0)
			{
				throw new InvalidOperationException("Invalid image file.");
			}

			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
			var extension = Path.GetExtension(image.FileName).ToLower();

			if (!allowedExtensions.Contains(extension))
			{
				throw new InvalidOperationException("Unsupported image format.");
			}

			var fileName = $"{Guid.NewGuid()}{extension}";
			var uploadFolder = Path.Combine(env.WebRootPath, "uploads", "ads");

			Directory.CreateDirectory(uploadFolder);

			var filePath = Path.Combine(uploadFolder, fileName);

			using (var stream = new FileStream(filePath, FileMode.Create))
			{
				await image.CopyToAsync(stream);
			}

			return $"/uploads/ads/{fileName}";
		}
		public async Task<AdDetailsModel?> GetDetailsAsync(int id)
		{
			return await context.Ads
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
					CreatedOn = a.CreatedOn,

					SellerName = a.User.UserName!,
					Tags = a.AdTags
	                    .Select(t => t.Tag.Name)
	                    .ToList(),

					ImageUrls = a.Images
						.OrderBy(i => i.Id)
						.Select(i => i.ImageUrl)
						.ToList(),

					CategoryPath = a.Category.ParentCategory != null
						? a.Category.ParentCategory.Name + " → " + a.Category.Name
						: a.Category.Name
				})
				.FirstOrDefaultAsync();
		}
		public async Task<IEnumerable<MyAdViewModel>> GetMyAdsAsync(string userId)
		{
			return await context.Ads
				.Where(a => a.UserId == userId)
				.OrderByDescending(a => a.CreatedOn)
				.Select(a => new MyAdViewModel
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
		public async Task<AdCreateModel?> GetEditModelAsync(int adId, string userId)
		{
			var ad = await context.Ads
				.Include(a => a.Images)
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
			ad.CategoryId = model.CategoryId;

			/* ================= IMAGES ================= */

			// Remove images that user removed in UI
			var imagesToRemove = ad.Images
				.Where(i => !model.ExistingImageUrls.Contains(i.ImageUrl))
				.ToList();

			foreach (var img in imagesToRemove)
			{
				ad.Images.Remove(img);

				// Optional: delete physical file later
				var physicalPath = Path.Combine(env.WebRootPath, img.ImageUrl.TrimStart('/'));
				if (File.Exists(physicalPath))
					File.Delete(physicalPath);
			}

			// Add newly uploaded images
			foreach (var image in model.Images)
			{
				var imageUrl = await SaveImageAsync(image);
				ad.Images.Add(new AdImage { ImageUrl = imageUrl });
			}

			// Enforce at least one image AFTER merge
			if (!ad.Images.Any())
				throw new InvalidOperationException("At least one image is required.");

			/* ================= TAGS ================= */

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
	}
}
