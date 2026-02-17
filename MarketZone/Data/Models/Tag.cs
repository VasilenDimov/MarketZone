using MarketZone.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace MarketZone.Data.Models
{
	public class Tag
	{
		public int Id { get; set; }

		[Required, StringLength(100)]
		public string Name { get; set; } = null!;

		public ICollection<AdTag> AdTags { get; set; } = new List<AdTag>();
	}
}
