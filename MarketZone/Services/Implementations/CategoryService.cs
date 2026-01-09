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
		public async Task<IEnumerable<CategoryChildDto>> GetChildrenAsync(int? parentId)
		{
			return await context.Categories
				.Where(c => c.ParentCategoryId == parentId)
				.Select(c => new CategoryChildDto
				{
					Id = c.Id,
					Name = c.Name,
					HasChildren = context.Categories.Any(sc => sc.ParentCategoryId == c.Id)
				})
				.ToListAsync();
		}

	}
}
