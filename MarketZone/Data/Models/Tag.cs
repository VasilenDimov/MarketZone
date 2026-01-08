using System.ComponentModel.DataAnnotations;

namespace MarketZone.Data.Models
{
	public class Tag
	{
		[Key]
		public int Id { get; set; }

		[Required, StringLength(100)]
		public string Name { get; set; } = null!;

		public ICollection<Ad> Ads { get; set; } = new List<Ad>();
	}

}
