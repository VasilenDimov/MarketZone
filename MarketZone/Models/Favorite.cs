using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketZone.Models
{
	public class Favorite
	{
		[Required]
		public string UserId { get; set; } = null!;

		[ForeignKey(nameof(UserId))]
		public User User { get; set; } = null!;

		[Required]
		public int AdId { get; set; }

		[ForeignKey(nameof(AdId))]
		public Ad Ad { get; set; } = null!;

		public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
	}
}
