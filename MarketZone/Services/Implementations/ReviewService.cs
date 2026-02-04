using MarketZone.Data;
using MarketZone.Data.Models;
using MarketZone.Services.Interfaces;
using MarketZone.ViewModels.Review;
using Microsoft.EntityFrameworkCore;

namespace MarketZone.Services.Implementations
{
	public class ReviewService : IReviewService
	{
		private readonly ApplicationDbContext context;

		public ReviewService(ApplicationDbContext context)
		{
			this.context = context;
		}

		public async Task AddReviewAsync(string reviewerId, ReviewCreateViewModel model)
		{
			if (reviewerId == model.ReviewedUserId)
				throw new InvalidOperationException("You cannot review yourself.");

			bool alreadyReviewed = await context.Reviews.AnyAsync(r =>
				r.ReviewerId == reviewerId &&
				r.ReviewedUserId == model.ReviewedUserId);

			if (alreadyReviewed)
				throw new InvalidOperationException("You have already reviewed this user.");

			var review = new Review
			{
				ReviewerId = reviewerId,
				ReviewedUserId = model.ReviewedUserId,
				Rating = model.Rating,
				Comment = model.Comment ?? string.Empty,
				CreatedOn = DateTime.UtcNow
			};

			context.Reviews.Add(review);
			await context.SaveChangesAsync();
		}

		public async Task<bool> CanReviewAsync(string reviewerId, string reviewedUserId)
		{
			if (reviewerId == reviewedUserId)
				return false;

			return !await context.Reviews.AnyAsync(r =>
				r.ReviewerId == reviewerId &&
				r.ReviewedUserId == reviewedUserId);
		}

		public async Task<IEnumerable<ReviewViewModel>> GetReviewsForUserAsync(string userId)
		{
			return await context.Reviews
				.AsNoTracking()
				.Where(r => r.ReviewedUserId == userId)
				.OrderByDescending(r => r.CreatedOn)
				.Select(r => new ReviewViewModel
				{
					Rating = r.Rating,
					Comment = r.Comment,
					CreatedOn = r.CreatedOn,
					ReviewerName = r.Reviewer.UserName!,
					ReviewerProfilePictureUrl = r.Reviewer.ProfilePictureUrl
				})
				.ToListAsync();
		}

		public async Task<double> GetAverageRatingAsync(string userId)
		{
			return await context.Reviews
				.Where(r => r.ReviewedUserId == userId)
				.Select(r => (double?)r.Rating)
				.AverageAsync() ?? 0;
		}

		public async Task<int> GetReviewCountAsync(string userId)
		{
			return await context.Reviews.CountAsync(r => r.ReviewedUserId == userId);
		}
	}
}
