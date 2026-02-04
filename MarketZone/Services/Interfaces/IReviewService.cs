using MarketZone.ViewModels.Review;

namespace MarketZone.Services.Interfaces
{
	public interface IReviewService
	{
		Task AddReviewAsync(string reviewerId, ReviewCreateViewModel model);

		Task<bool> CanReviewAsync(string reviewerId, string reviewedUserId);

		Task<IEnumerable<ReviewViewModel>> GetReviewsForUserAsync(string userId);

		Task<double> GetAverageRatingAsync(string userId);

		Task<int> GetReviewCountAsync(string userId);
	}
}
