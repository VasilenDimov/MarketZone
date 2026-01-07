using MarketZone.ViewModels.Ad;

namespace MarketZone.Services.Interfaces
{
	public interface IAdService
	{
		Task<int> CreateAsync(AdCreateModel model, string userId);
	}
}
