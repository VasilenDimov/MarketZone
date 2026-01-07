using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MarketZone.Models
{
	public class User : IdentityUser
	{
		[StringLength(250)]
		public string ProfilePictureUrl { get; set; } = null!;

		public ICollection<Ad> Ads { get; set; } = new List<Ad>();
		public ICollection<Message> SentMessages { get; set; } = new List<Message>();
		public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
		public ICollection<Review> ReviewsWritten { get; set; } = new List<Review>();
		public ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
		public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

	}
}
