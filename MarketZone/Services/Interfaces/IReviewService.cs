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

		Task<ReviewEditViewModel?> GetForEditAsync(int reviewId);

		Task<bool> UpdateAsync(ReviewEditViewModel model, string reviewerId);

		Task<bool> DeleteAsync(int reviewId, string reviewerId);

		Task<bool> CanUserEditAsync(int reviewId, string reviewerId);
	}
}
