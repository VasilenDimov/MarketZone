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

		[Required, StringLength(4000)]
		public string Description { get; set; } = null!;
		
		[Required]
		public decimal Price { get; set; }

		[Required]
		public Currency Currency { get; set; }

		[Required, StringLength(200)]
		public string Address { get; set; } = null!;
		public double? Latitude { get; set; }  
		public double? Longitude { get; set; }

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

		public AdStatus Status { get; set; } = AdStatus.Pending;
		public DateTime? ReviewedOn { get; set; }
		public string? ReviewedByUserId { get; set; }
		public User? ReviewedByUser { get; set; }
		public string? RejectionReason { get; set; }

		public ICollection<Message> Messages { get; set; } = new List<Message>();
		public ICollection<AdImage> Images { get; set; } = new List<AdImage>();
		public ICollection<AdTag> AdTags { get; set; } = new List<AdTag>();

	}
}
