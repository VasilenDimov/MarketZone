using MarketZone.Data.Enums;
using MarketZone.Models.Enums;
using MarketZone.ViewModels.Category;
using System.ComponentModel.DataAnnotations;

namespace MarketZone.ViewModels.Ad
{
	public class AdCreateModel
	{
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

		[Required(ErrorMessage = "Please select a category.")]
		[Range(1, int.MaxValue, ErrorMessage = "Please select a valid category.")]
		public int? CategoryId { get; set; }

		public IEnumerable<CategorySelectModel> Categories { get; set; }
			= new List<CategorySelectModel>();
		public List<string> ExistingImageUrls { get; set; } = new();

		public List<IFormFile> Images { get; set; } = new();

		[Required(ErrorMessage = "Please select an address.")]
		[StringLength(200)]
		public string Address { get; set; } = null!;
		public double? Latitude { get; set; }
		public double? Longitude { get; set; }

		[Required]
		public ItemCondition Condition { get; set; }

		[MaxLength(500)]
		public string? Tags { get; set; }
	}
}
