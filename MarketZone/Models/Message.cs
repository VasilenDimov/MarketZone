using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketZone.Models
{
	public class Message
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[StringLength(1000)]
		public string Content { get; set; } = null!;

		public DateTime SentOn { get; set; } = DateTime.Now;

		// Foreign Keys
		[Required]
		public string SenderId { get; set; }

		[ForeignKey(nameof(SenderId))]
		public User Sender { get; set; } = null!;

		[Required]
		public string ReceiverId { get; set; }

		[ForeignKey(nameof(ReceiverId))]
		public User Receiver { get; set; } = null!;

		[Required]
		public int AdId { get; set; }

		[ForeignKey(nameof(AdId))]
		public Ad Ad { get; set; } = null!;

	}
}
