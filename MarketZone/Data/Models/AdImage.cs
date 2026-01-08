using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MarketZone.Data.Models
{
	public class AdImage
	{
		[Key]
		public int Id { get; set; }
			
		[Required]
		public string ImageUrl { get; set; } = null!;

		[Required]
		public int AdId { get; set; }

		[ForeignKey(nameof(AdId))]
		public Ad Ad { get; set; } = null!;
	}

}
