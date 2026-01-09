using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketZone.Data.Models
{
	public class Review
	{
		[Key]
		public int Id { get; set; }

		[Range(1, 5)]
		public int Rating { get; set; }

		[StringLength(1000)]
		public string Comment { get; set; } = null!;

		public DateTime CreatedOn { get; set; } = DateTime.Now;

		// Foreign Keys
		[Required]
		public string ReviewerId { get; set; } = null!;

		[ForeignKey(nameof(ReviewerId))]
		public User Reviewer { get; set; } = null!;

		[Required]
		public string ReviewedUserId { get; set; } = null!;

		[ForeignKey(nameof(ReviewedUserId))]
		public User ReviewedUser { get; set; } = null!;

	}
}
