using MarketZone.ViewModels.Ad;

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
