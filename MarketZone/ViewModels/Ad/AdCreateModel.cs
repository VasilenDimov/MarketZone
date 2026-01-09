using MarketZone.Data.Enums;
using MarketZone.Models.Enums;
using MarketZone.ViewModels.Category;
using System.ComponentModel.DataAnnotations;

namespace MarketZone.ViewModels.Ad
{
	public class AdCreateModel
	{
		[Required]
		[StringLength(100)]
		public string Title { get; set; } = null!;

		[Required]
		[MinLength(40)]
		[MaxLength(5000)]
		public string Description { get; set; } = null!;


		[Required]
		[Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
		public decimal Price { get; set; }

		[Required]
		[Display(Name = "Currency")]
		public Currency Currency { get; set; }

		[Required]
		[Display(Name = "Category")]
		public int CategoryId { get; set; }

		// Dropdown data (NOT saved)
		public IEnumerable<CategorySelectModel> Categories { get; set; }
			= new List<CategorySelectModel>();

		[Required]
		[StringLength(200)]
		public string Address { get; set; } = null!;

		// Images
		[Required]
		[MinLength(1, ErrorMessage = "At least one image is required.")]
		public List<IFormFile> Images { get; set; } = new();

		[Required]
		[Display(Name = "Condition")]
		public ItemCondition Condition { get; set; }

		// Optional tags (comma-separated from UI)
		[MaxLength(500)]
		public string? Tags { get; set; }
	}
}
