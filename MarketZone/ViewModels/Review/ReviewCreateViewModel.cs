using System.ComponentModel.DataAnnotations;

namespace MarketZone.ViewModels.Review
{
	public class ReviewCreateViewModel
	{
		[Range(1, 5)]
		public int Rating { get; set; }

		[StringLength(1000)]
		public string? Comment { get; set; }

		public string ReviewedUserId { get; set; } = null!;
	}
}
