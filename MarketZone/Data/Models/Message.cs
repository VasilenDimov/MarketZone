using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketZone.Data.Models
{
	public class Message
	{
		[Key]
		public int Id { get; set; }

		[StringLength(1000)]
		public string Content { get; set; } = string.Empty;

		public DateTime SentOn { get; set; } = DateTime.UtcNow;

		// Foreign Keys
		[Required]
		public string SenderId { get; set; } = null!;

		[ForeignKey(nameof(SenderId))]
		public User Sender { get; set; } = null!;

		[Required]
		public string ReceiverId { get; set; } = null!;

		[ForeignKey(nameof(ReceiverId))]
		public User Receiver { get; set; } = null!;

		[Required]
		public int AdId { get; set; }

		[ForeignKey(nameof(AdId))]
		public Ad Ad { get; set; } = null!;
		public ICollection<MessageImage> Images { get; set; } = new List<MessageImage>();

	}
}
