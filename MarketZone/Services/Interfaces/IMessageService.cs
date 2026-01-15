using MarketZone.ViewModels.Message;

namespace MarketZone.Services.Interfaces
{
	public interface IMessageService
	{
		Task<ChatViewModel?> GetChatAsync(int adId, string userId);
		Task SaveMessageAsync(int adId, string senderId, string content);
		Task<InboxViewModel> GetInboxAsync(string userId,string mode);

	}

}
