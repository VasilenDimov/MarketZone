namespace MarketZone.ViewModels.Ad
{
	public class AdSearchViewModel : SearchFiltersBaseViewModel
	{

		public IEnumerable<AdListItemViewModel> Ads { get; set; }
			= new List<AdListItemViewModel>();
	}
}
