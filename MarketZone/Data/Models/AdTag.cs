using System.ComponentModel.DataAnnotations.Schema;

namespace MarketZone.Data.Models
{
	public class AdTag
	{
		public int AdId { get; set; }
		public Ad Ad { get; set; } = null!;

		public int TagId { get; set; }
		public Tag Tag { get; set; } = null!;
	}
}
