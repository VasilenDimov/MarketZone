using System.ComponentModel.DataAnnotations;

namespace MarketZone.ViewModels.Review
{
	public abstract class ReviewFormViewModel
	{
		[Required]
		[Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
		public int Rating { get; set; }
		public string? Comment { get; set; }
		public string ReviewedUserId { get; set; } = null!;
	}
}
