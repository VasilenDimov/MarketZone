using MarketZone.Data.Enums;
using MarketZone.Models.Enums;

namespace MarketZone.ViewModels.Ad
{
	public class AdDetailsModel
	{
		public int Id { get; set; }

		public string Title { get; set; } = null!;
		public string Description { get; set; } = null!;

		public decimal Price { get; set; }
		public Currency Currency { get; set; }

		public ItemCondition Condition { get; set; }

		public bool IsFavorite { get; set; }

		public string CategoryPath { get; set; } = null!;
		public string Address { get; set; } = null!;
		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
		public DateTime CreatedOn { get; set; }

		public string SellerId { get; set; } = null!;
		public string SellerName { get; set; } = null!;

		public List<string> ImageUrls { get; set; } = new();
	}

}
