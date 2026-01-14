using MarketZone.Data;
using MarketZone.Data.Models;
using MarketZone.Services.Interfaces;
using MarketZone.ViewModels.Ad;
using Microsoft.EntityFrameworkCore;

public class FavoriteService : IFavoriteService
{
	private readonly ApplicationDbContext context;

	public FavoriteService(ApplicationDbContext context)
	{
		this.context = context;
	}

	public async Task<bool> ToggleAsync(int adId, string userId)
	{
		var favorite = await context.Favorites
			.FirstOrDefaultAsync(f => f.AdId == adId && f.UserId == userId);

		if (favorite == null)
		{
			context.Favorites.Add(new Favorite
			{
				AdId = adId,
				UserId = userId
			});

			await context.SaveChangesAsync();
			return true;
		}

		context.Favorites.Remove(favorite);
		await context.SaveChangesAsync();
		return false;
	}

	public async Task<bool> IsFavoriteAsync(int adId, string userId)
	{
		return await context.Favorites.AnyAsync(f =>
			f.AdId == adId && f.UserId == userId);
	}

	public async Task<IEnumerable<AdListItemViewModel>> GetFavoritesAsync(string userId)
	{
		return await context.Favorites
			.Where(f => f.UserId == userId)
			.Join(
				context.Ads,
				f => f.AdId,
				a => a.Id,
				(f, a) => new AdListItemViewModel
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
			.AsNoTracking()
			.ToListAsync();
	}
}
