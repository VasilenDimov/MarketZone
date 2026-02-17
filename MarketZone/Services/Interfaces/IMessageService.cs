using MarketZone.ViewModels.Message;

namespace MarketZone.Services.Interfaces
{
	public interface IMessageService
	{
		Task<ChatViewModel?> GetChatAsync(int adId, string currentUserId, string otherUserId);

		Task SaveMessageAsync(
			int adId,
			string senderId,
			string receiverId,
			string? content,
			List<string> imageUrls);

		Task<InboxViewModel> GetInboxAsync(string userId, string mode);
	}
}
