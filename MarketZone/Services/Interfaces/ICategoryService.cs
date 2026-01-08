using MarketZone.ViewModels.Category;

namespace MarketZone.Services.Interfaces
{
	public interface ICategoryService
	{
		Task<IEnumerable<CategorySelectModel>> GetAllAsync();
	}
}
