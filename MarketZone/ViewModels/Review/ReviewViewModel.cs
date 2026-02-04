namespace MarketZone.ViewModels.Review
{
	public class ReviewViewModel
	{
		public int Rating { get; set; }
		public string? Comment { get; set; }
		public DateTime CreatedOn { get; set; }

		public string ReviewerName { get; set; } = null!;
		public string ReviewerProfilePictureUrl { get; set; } = "/images/default-avatar.png";
	}
}
