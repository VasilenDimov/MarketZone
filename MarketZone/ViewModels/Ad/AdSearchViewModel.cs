namespace MarketZone.ViewModels.Ad
{
	public class AdSearchViewModel
	{
		public string? SearchTerm { get; set; }

		public int CurrentPage { get; set; } = 1;

		public int TotalPages { get; set; }

		public IEnumerable<AdListItemViewModel> Ads { get; set; }
			= new List<AdListItemViewModel>();
	}
}
