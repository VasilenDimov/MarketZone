using System.ComponentModel.DataAnnotations;

namespace MarketZone.Data.Models
{
	public class Favorite
	{
		[Required]
		public string UserId { get; set; } = null!;

		[Required]
		public int AdId { get; set; }

		public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
	}
}
