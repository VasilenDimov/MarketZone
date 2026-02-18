namespace MarketZone.ViewModels.Ad
{
	public abstract class SearchFiltersBaseViewModel
	{
		public string? SearchTerm { get; set; }
		public string? Address { get; set; }

		public int? CategoryId { get; set; }
		public string? CategoryName { get; set; }

		public decimal? MinPrice { get; set; }
		public decimal? MaxPrice { get; set; }

		public string? Tags { get; set; }
		public string? Sort { get; set; }

		public int CurrentPage { get; set; } = 1;
		public int TotalPages { get; set; }
	}
}
