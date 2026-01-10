using MarketZone.Data.Models;

public class EmailVerificationCode
{
	public int Id { get; set; }

	public string UserId { get; set; } = null!;
	public User User { get; set; } = null!;

	public string Code { get; set; } = null!; // "123456"
	public DateTime CreatedAt { get; set; }
	public DateTime ExpiresAt { get; set; }
}
