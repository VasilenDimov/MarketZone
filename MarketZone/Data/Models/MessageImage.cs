using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketZone.Data.Models
{
	public class MessageImage
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string ImageUrl { get; set; } = null!;

		[Required]
		public int MessageId { get; set; }

		[ForeignKey(nameof(MessageId))]
		public Message Message { get; set; } = null!;
	}

}
