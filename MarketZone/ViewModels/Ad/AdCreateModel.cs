using System.ComponentModel.DataAnnotations;

namespace MarketZone.ViewModels.Ad
{
	public class AdCreateModel
	{
		[Required]
		[StringLength(100)]
		public string Title { get; set; } = null!;

		[Required]
		[StringLength(1000)]
		public string Description { get; set; } = null!;

		[Required]
		[Range(0, double.MaxValue)]
		public decimal Price { get; set; }

		[Required]
		public int CategoryId { get; set; }
	}
}
