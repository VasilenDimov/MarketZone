using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MarketZone.Data.Models
{
	public class User : IdentityUser
	{
		public string? ProfilePictureUrl { get; set; }
		public DateTime CreatedOn { get; set; }
		public DateTime? LastOnlineOn { get; set; }

		public bool IsDeleted { get; set; }
		public DateTime? DeletedOn { get; set; }

		public ICollection<Ad> Ads { get; set; } = new List<Ad>();
		public ICollection<Message> SentMessages { get; set; } = new List<Message>();
		public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
		public ICollection<Review> ReviewsWritten { get; set; } = new List<Review>();
		public ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();

	}
}
