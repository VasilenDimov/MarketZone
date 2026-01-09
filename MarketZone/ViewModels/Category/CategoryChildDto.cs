namespace MarketZone.ViewModels.Category
{
	public class CategoryChildDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = null!;
		public bool HasChildren { get; set; }
	}

}
