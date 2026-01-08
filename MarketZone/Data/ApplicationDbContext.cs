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

			builder.Entity<Ad>()
	            .Property(a => a.Price)
	            .HasPrecision(18, 2);

			builder.Entity<Favorite>()
	            .HasKey(f => new { f.UserId, f.AdId })
				.IsClustered(false);

			builder.Entity<Favorite>()
		         .HasOne(f => f.User)
		         .WithMany(u => u.Favorites)
		         .HasForeignKey(f => f.UserId)
		         .OnDelete(DeleteBehavior.NoAction);

			builder.Entity<Favorite>()
				.HasOne(f => f.Ad)
				.WithMany(a => a.FavoritedBy)
				.HasForeignKey(f => f.AdId)
				.OnDelete(DeleteBehavior.Cascade);

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

		}
	}
}
