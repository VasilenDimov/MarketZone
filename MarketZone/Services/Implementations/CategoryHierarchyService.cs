using MarketZone.Data;
using MarketZone.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarketZone.Services.Implementations
{
	public class CategoryHierarchyService : ICategoryHierarchyService
	{
		private readonly ApplicationDbContext context;

		public CategoryHierarchyService(ApplicationDbContext context)
		{
			this.context = context;
		}

		public async Task<List<int>> GetDescendantCategoryIdsAsync(int rootId)
		{
			var categories = await context.Categories
				.AsNoTracking()
				.Select(c => new { c.Id, c.ParentCategoryId })
				.ToListAsync();

			var result = new HashSet<int>();
			var queue = new Queue<int>();

			queue.Enqueue(rootId);
			result.Add(rootId);

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();

				var children = categories
					.Where(c => c.ParentCategoryId == current)
					.Select(c => c.Id);

				foreach (var child in children)
				{
					if (result.Add(child))
						queue.Enqueue(child);
				}
			}

			return result.ToList();
		}
	}
}
