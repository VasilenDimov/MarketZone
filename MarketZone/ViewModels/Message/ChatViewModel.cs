namespace MarketZone.ViewModels.Message
{
	public class ChatViewModel
	{
		public int AdId { get; set; }

		// Unique per conversation (Ad + 2 users)
		public string ChatId { get; set; } = null!;

		// The other participant in THIS conversation
		public string OtherUserId { get; set; } = null!;
		public string OtherUserName { get; set; } = null!;
		public string CurrentUserId { get; set; } = null!;
		public string OtherUserProfilePictureUrl { get; set; } = null!;

		public List<ChatMessageViewModel> Messages { get; set; } = new();

		// Ad preview (for chat header)
		public string AdTitle { get; set; } = null!;
		public string AdImageUrl { get; set; } = null!;
	}
}
