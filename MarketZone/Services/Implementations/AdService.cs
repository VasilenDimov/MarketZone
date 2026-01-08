using MarketZone.Data;
using MarketZone.Data.Models;
using MarketZone.Services.Interfaces;
using MarketZone.ViewModels.Ad;

namespace MarketZone.Services.Implementations
{
	public class AdService : IAdService
	{
		private readonly ApplicationDbContext context;

		public AdService(ApplicationDbContext context)
		{
			this.context = context;
		}

		public async Task<int> CreateAsync(AdCreateModel model, string userId)
		{
			var ad = new Ad
			{
				Title = model.Title,
				Description = model.Description,
				Price = model.Price,
				CategoryId = model.CategoryId,
				UserId = userId,
				CreatedOn = DateTime.UtcNow
			};

			await context.Ads.AddAsync(ad);
			await context.SaveChangesAsync();

			return ad.Id;
		}
	}
}
