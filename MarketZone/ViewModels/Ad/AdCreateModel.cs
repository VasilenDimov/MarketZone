using MarketZone.Data.Enums;
using MarketZone.Models.Enums;
using MarketZone.ViewModels.Category;
using System.ComponentModel.DataAnnotations;

namespace MarketZone.ViewModels.Ad
{
	public class AdCreateModel
	{
		// Used only for Edit
		public int Id { get; set; }

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
		public Currency Currency { get; set; }

		[Required]
		public int CategoryId { get; set; }

		// Dropdown data (not saved)
		public IEnumerable<CategorySelectModel> Categories { get; set; }
			= new List<CategorySelectModel>();

		// Existing DB images (Edit only)
		public List<string> ExistingImageUrls { get; set; } = new();

		// New uploaded images
		public List<IFormFile> Images { get; set; } = new();

		[Required]
		[StringLength(200)]
		public string Address { get; set; } = null!;

		[Required]
		public ItemCondition Condition { get; set; }

		// Optional tags
		[MaxLength(500)]
		public string? Tags { get; set; }
	}
}
