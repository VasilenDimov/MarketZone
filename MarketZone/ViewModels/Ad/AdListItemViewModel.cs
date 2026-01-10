using MarketZone.Data.Enums;

namespace MarketZone.ViewModels.Ad
{
	public class AdListItemViewModel
	{
		public int Id { get; set; }

		public string Title { get; set; } = null!;

		public decimal Price { get; set; }

		public Currency Currency { get; set; }

		public string? MainImageUrl { get; set; }

		public DateTime CreatedOn { get; set; }
	}
}
