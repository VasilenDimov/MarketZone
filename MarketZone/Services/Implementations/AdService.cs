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
	}
}
