namespace MarketZone.ViewModels.Category
{
	public class CategoryPathDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = null!;
		public int? ParentCategoryId { get; set; }
	}
}