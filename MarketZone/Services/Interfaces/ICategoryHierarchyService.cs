namespace MarketZone.Services.Interfaces
{
	public interface ICategoryHierarchyService
	{
		Task<List<int>> GetDescendantCategoryIdsAsync(int rootId);
	}
}
