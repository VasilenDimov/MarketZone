using MarketZone.ViewModels.Category;

namespace MarketZone.ViewModels.Ad
{
	public class AdSearchViewModel
	{
		public string? SearchTerm { get; set; }

		// Filter properties
		public decimal? MinPrice { get; set; }
		public decimal? MaxPrice { get; set; }
		public int? CategoryId { get; set; }
		public string? Tags { get; set; } // Comma-separated
		public string? Address { get; set; }
		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
		public double? RadiusKm { get; set; } = 10; // Default 10km radius

		public int CurrentPage { get; set; } = 1;

		public int TotalPages { get; set; }

		public IEnumerable<AdListItemViewModel> Ads { get; set; }
			= new List<AdListItemViewModel>();

		// For category picker
		public IEnumerable<CategorySelectModel> Categories { get; set; }
			= new List<CategorySelectModel>();
	}
}
