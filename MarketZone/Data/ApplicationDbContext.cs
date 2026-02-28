using MarketZone.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MarketZone.Data
{
	public class ApplicationDbContext : IdentityDbContext<User>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options) { }

		public DbSet<Category> Categories { get; set; } = null!;
		public DbSet<Ad> Ads { get; set; } = null!;
		public DbSet<Message> Messages { get; set; } = null!;
		public DbSet<Review> Reviews { get; set; } = null!;
		public DbSet<AdImage> AdImages { get; set; } = null!;
		public DbSet<Tag> Tags { get; set; } = null!;
		public DbSet<Favorite> Favorites { get; set; } = null!;
		public DbSet<AdTag> AdTags { get; set; } = null!;
		public DbSet<MessageImage> MessageImages { get; set; } = null!;

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// Message -> Sender
			builder.Entity<Message>()
				.HasOne(m => m.Sender)
				.WithMany(u => u.SentMessages)
				.HasForeignKey(m => m.SenderId)
				.OnDelete(DeleteBehavior.Restrict);

			// Message -> Receiver
			builder.Entity<Message>()
				.HasOne(m => m.Receiver)
				.WithMany(u => u.ReceivedMessages)
				.HasForeignKey(m => m.ReceiverId)
				.OnDelete(DeleteBehavior.Restrict);

			// Review -> Reviewer
			builder.Entity<Review>()
				.HasOne(r => r.Reviewer)
				.WithMany(u => u.ReviewsWritten)
				.HasForeignKey(r => r.ReviewerId)
				.OnDelete(DeleteBehavior.Restrict);

			// Review -> ReviewedUser
			builder.Entity<Review>()
				.HasOne(r => r.ReviewedUser)
				.WithMany(u => u.ReviewsReceived)
				.HasForeignKey(r => r.ReviewedUserId)
				.OnDelete(DeleteBehavior.Restrict);

			// Composite primary key: one review per user per user
			builder.Entity<Review>()
				.HasIndex(r => new { r.ReviewerId, r.ReviewedUserId })
				.IsUnique();

			builder.Entity<Ad>()
				.Property(a => a.Price)
				.HasPrecision(18, 2);

			// Composite primary key: one favorite per user per ad
			builder.Entity<Favorite>()
				.HasKey(f => new { f.UserId, f.AdId });

			builder.Entity<AdTag>()
				.HasKey(at => new { at.AdId, at.TagId });

			builder.Entity<AdTag>()
				.HasOne(at => at.Ad)
				.WithMany(a => a.AdTags)
				.HasForeignKey(at => at.AdId);

			builder.Entity<AdTag>()
				.HasOne(at => at.Tag)
				.WithMany(t => t.AdTags)
				.HasForeignKey(at => at.TagId);

			builder.Entity<Ad>()
				.HasOne(a => a.ReviewedByUser)
				.WithMany()
				.HasForeignKey(a => a.ReviewedByUserId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<Category>().HasData(
	// Real Estate
	new Category { Id = 1, Name = "Real Estate", ParentCategoryId = null },
	new Category { Id = 2, Name = "Sales", ParentCategoryId = 1 },
	new Category { Id = 3, Name = "Rentals", ParentCategoryId = 1 },
	new Category { Id = 4, Name = "Apartments", ParentCategoryId = 2 },
	new Category { Id = 5, Name = "Houses", ParentCategoryId = 2 },
	new Category { Id = 6, Name = "Land / Plots", ParentCategoryId = 2 },
	new Category { Id = 7, Name = "Commercial", ParentCategoryId = 2 },
	new Category { Id = 8, Name = "Rooms", ParentCategoryId = 3 },
	new Category { Id = 9, Name = "Apartments", ParentCategoryId = 3 },
	new Category { Id = 10, Name = "Houses", ParentCategoryId = 3 },
	new Category { Id = 11, Name = "Garages / Parking", ParentCategoryId = 3 },

	// Vehicles
	new Category { Id = 12, Name = "Vehicles", ParentCategoryId = null },
	new Category { Id = 13, Name = "Cars", ParentCategoryId = 12 },
	new Category { Id = 14, Name = "Motorcycles", ParentCategoryId = 12 },
	new Category { Id = 15, Name = "Trucks", ParentCategoryId = 12 },
	new Category { Id = 16, Name = "Vans", ParentCategoryId = 12 },
	new Category { Id = 17, Name = "Bicycles", ParentCategoryId = 12 },
	new Category { Id = 18, Name = "Parts & Accessories", ParentCategoryId = 12 },

	// Electronics
	new Category { Id = 19, Name = "Electronics", ParentCategoryId = null },
	new Category { Id = 20, Name = "Phones", ParentCategoryId = 19 },
	new Category { Id = 21, Name = "Computers & Laptops", ParentCategoryId = 19 },
	new Category { Id = 22, Name = "TV / Audio / Video", ParentCategoryId = 19 },
	new Category { Id = 23, Name = "Gaming", ParentCategoryId = 19 },
	new Category { Id = 24, Name = "Cameras", ParentCategoryId = 19 },
	new Category { Id = 25, Name = "Smart Home", ParentCategoryId = 19 },
	new Category { Id = 26, Name = "Accessories", ParentCategoryId = 19 },

	// Home & Garden
	new Category { Id = 27, Name = "Home & Garden", ParentCategoryId = null },
	new Category { Id = 28, Name = "Furniture", ParentCategoryId = 27 },
	new Category { Id = 29, Name = "Home Appliances", ParentCategoryId = 27 },
	new Category { Id = 30, Name = "Kitchen & Dining", ParentCategoryId = 27 },
	new Category { Id = 31, Name = "Tools", ParentCategoryId = 27 },
	new Category { Id = 32, Name = "Garden", ParentCategoryId = 27 },
	new Category { Id = 33, Name = "Decor", ParentCategoryId = 27 },

	// Fashion
	new Category { Id = 34, Name = "Fashion", ParentCategoryId = null },

	new Category { Id = 35, Name = "Women", ParentCategoryId = 34 },
	new Category { Id = 36, Name = "Shoes", ParentCategoryId = 35 },
	new Category { Id = 37, Name = "Clothes", ParentCategoryId = 35 },

	new Category { Id = 38, Name = "Men", ParentCategoryId = 34 },
	new Category { Id = 39, Name = "Shoes", ParentCategoryId = 38 },
	new Category { Id = 40, Name = "Clothes", ParentCategoryId = 38 },

	new Category { Id = 41, Name = "Kids", ParentCategoryId = 34 },
	new Category { Id = 42, Name = "Shoes", ParentCategoryId = 41 },
	new Category { Id = 43, Name = "Clothes", ParentCategoryId = 41 },

	new Category { Id = 44, Name = "Watches & Jewelry", ParentCategoryId = 34 },

	// Pets
	new Category { Id = 45, Name = "Pets", ParentCategoryId = null },
	new Category { Id = 46, Name = "Dogs", ParentCategoryId = 45 },
	new Category { Id = 47, Name = "Cats", ParentCategoryId = 45 },
	new Category { Id = 48, Name = "Birds", ParentCategoryId = 45 },
	new Category { Id = 49, Name = "Fish", ParentCategoryId = 45 },
	new Category { Id = 50, Name = "Pet Supplies", ParentCategoryId = 45 },

	// Sports & Hobby
	new Category { Id = 51, Name = "Sports & Hobby", ParentCategoryId = null },
	new Category { Id = 52, Name = "Fitness", ParentCategoryId = 51 },
	new Category { Id = 53, Name = "Outdoor", ParentCategoryId = 51 },
	new Category { Id = 54, Name = "Bikes & Cycling", ParentCategoryId = 51 },
	new Category { Id = 55, Name = "Musical Instruments", ParentCategoryId = 51 },
	new Category { Id = 56, Name = "Books", ParentCategoryId = 51 },

	// Baby & Kids
	new Category { Id = 57, Name = "Baby & Kids", ParentCategoryId = null },
	new Category { Id = 58, Name = "Baby Gear", ParentCategoryId = 57 },
	new Category { Id = 59, Name = "Toys", ParentCategoryId = 57 },
	new Category { Id = 60, Name = "Kids Clothing", ParentCategoryId = 57 },
	new Category { Id = 61, Name = "Strollers", ParentCategoryId = 57 }
);
		}
	}
}