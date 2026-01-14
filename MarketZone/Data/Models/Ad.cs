using MarketZone.Data.Enums;
using MarketZone.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketZone.Data.Models
{
	public class Ad
	{
		[Key]
		public int Id { get; set; }

		[Required, StringLength(100)]
		public string Title { get; set; } = null!;

		[Required, StringLength(5000)]
		public string Description { get; set; } = null!;
		
		[Required]
		public decimal Price { get; set; }

		[Required]
		public Currency Currency { get; set; }

		[Required, StringLength(200)]
		public string Address { get; set; } = null!;

		[Required]
		public ItemCondition Condition { get; set; }
		public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

		// Foreign Keys		
		[Required]
		public int CategoryId { get; set; }

		[ForeignKey(nameof(CategoryId))]
		public Category Category { get; set; } = null!;

		[Required]
		public string UserId { get; set; } = null!;

		[ForeignKey(nameof(UserId))]
		public User User { get; set; } = null!;

		public ICollection<Message> Messages { get; set; } = new List<Message>();
		public ICollection<AdImage> Images { get; set; } = new List<AdImage>();
		public ICollection<AdTag> AdTags { get; set; } = new List<AdTag>();

	}
}
