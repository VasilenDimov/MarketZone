using MarketZone.Data;
using MarketZone.Services.Interfaces;
using MarketZone.ViewModels.Category;
using Microsoft.EntityFrameworkCore;

namespace MarketZone.Services.Implementations
{
	public class CategoryService : ICategoryService
	{
		private readonly ApplicationDbContext context;

		public CategoryService(ApplicationDbContext context)
		{
			this.context = context;
		}

		public async Task<IEnumerable<CategorySelectModel>> GetAllAsync()
		{
			return await context.Categories
				.OrderBy(c => c.Name)
				.Select(c => new CategorySelectModel
				{
					Id = c.Id,
					Name = c.Name
				})
				.ToListAsync();
		}
	}
}
