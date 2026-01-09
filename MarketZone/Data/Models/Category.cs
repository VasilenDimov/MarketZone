using MarketZone.Data.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Category
{
	public int Id { get; set; }

	[Required]
	[StringLength(100)]
	public string Name { get; set; } = null!;

	public int? ParentCategoryId { get; set; }

	[ForeignKey(nameof(ParentCategoryId))]
	public Category? ParentCategory { get; set; }

	public ICollection<Category> SubCategories { get; set; }
		= new List<Category>();

	public ICollection<Ad> Ads { get; set; }
		= new List<Ad>();
}
