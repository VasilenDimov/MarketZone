using Microsoft.Build.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketZone.Models
{
	public class Ad
	{
		[Key]
		public int Id { get; set; }

		[Required, StringLength(100)]
		public string Title { get; set; } = null!;

		[Required, StringLength(1000)]
		public string Description { get; set; } = null!;
		
		[Required, Range(0, double.MaxValue)]
		public decimal Price { get; set; }

		[StringLength(250)]
		public string ImageUrl { get; set; } = null!;

		public DateTime CreatedOn { get; set; } = DateTime.Now;

		// Foreign Keys		
		[Required]
		public int CategoryId { get; set; }

		[ForeignKey(nameof(CategoryId))]
		public Category Category { get; set; } = null!;

		[Required]
		public string UserId { get; set; }

		[ForeignKey(nameof(UserId))]
		public User User { get; set; } = null!;

		public ICollection<Message> Messages { get; set; } = new List<Message>();
	}
}
