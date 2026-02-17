using MarketZone.Data.Models;

namespace MarketZone.Data.Models
{
	public class EmailVerificationCode
	{
		public int Id { get; set; }
		public string Code { get; set; } = null!;
		public DateTime CreatedAt { get; set; }
		public DateTime ExpiresAt { get; set; }

		public string UserId { get; set; } = null!;
		public User User { get; set; } = null!;
	}
}
