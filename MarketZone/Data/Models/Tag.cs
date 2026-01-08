using MarketZone.Data.Models;
using System.ComponentModel.DataAnnotations;

public class Tag
{
	public int Id { get; set; }

	[Required, StringLength(100)]
	public string Name { get; set; } = null!;

	public ICollection<AdTag> AdTags { get; set; } = new List<AdTag>();
}
