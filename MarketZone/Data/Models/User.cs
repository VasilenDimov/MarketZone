using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MarketZone.Data.Models
{
	public class User : IdentityUser
	{
		[Required]
		public string ProfilePictureUrl { get; set; } = "/images/default-avatar.png";

		public ICollection<Ad> Ads { get; set; } = new List<Ad>();
		public ICollection<Message> SentMessages { get; set; } = new List<Message>();
		public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
		public ICollection<Review> ReviewsWritten { get; set; } = new List<Review>();
		public ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();

	}
}
