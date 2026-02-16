using MarketZone.ViewModels.Ad;
using MarketZone.ViewModels.Category;
using MarketZone.ViewModels.Review;

namespace MarketZone.ViewModels.User
{
	public class UserProfileViewModel
	{
		// User info
		public string UserId { get; set; } = null!;
		public string UserName { get; set; } = null!;
		public string Email { get; set; } = null!;
		public string ProfilePictureUrl { get; set; } = "/images/default-avatar.png";

		public DateTime CreatedOn { get; set; }
		public DateTime? LastOnlineOn { get; set; }

		// Ads
		public IEnumerable<AdListItemViewModel> Ads { get; set; }
			= new List<AdListItemViewModel>();

		// Search & sort
		public string? SearchTerm { get; set; }
		public string? Sort { get; set; }

		// Filter properties
		public decimal? MinPrice { get; set; }
		public decimal? MaxPrice { get; set; }
		public int? CategoryId { get; set; }
		public string? Tags { get; set; }
		public string? Address { get; set; }
		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
		public double? RadiusKm { get; set; } = 10;

		public IEnumerable<CategorySelectModel> Categories { get; set; }
			= new List<CategorySelectModel>();

		//Reviews
		public IEnumerable<ReviewViewModel> Reviews { get; set; }
			= new List<ReviewViewModel>();

		public double AverageRating { get; set; }
		public int ReviewCount { get; set; }
		public bool CanReview { get; set; }

		//Last Online Dislay
		public string? LastOnlineDisplay =>
	    LastOnlineOn == null
		? "Never"
		: FormatLastOnline(LastOnlineOn.Value);

		private static string FormatLastOnline(DateTime date)
		{
			var now = DateTime.UtcNow;

			if (date.Date == now.Date)
				return date.ToString("HH:mm");

			if (date.Date == now.AddDays(-1).Date)
				return $"Yesterday";

			if (date.Year == now.Year)
				return date.ToString("dd MMM");

			return date.ToString("yyyy MMM dd");
		}

	}

}
