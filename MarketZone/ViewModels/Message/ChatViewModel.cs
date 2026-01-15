namespace MarketZone.ViewModels.Message
{
	public class ChatViewModel
	{
		public int AdId { get; set; }
		public string ChatId { get; set; } = null!;
		public string OtherUserName { get; set; } = null!;
		public string CurrentUserId { get; set; } = null!;
		public List<ChatMessageViewModel> Messages { get; set; } = new();

		// Ad preview (for chat header)
		public string AdTitle { get; set; } = null!;
		public string AdImageUrl { get; set; } = null!;
	}
}
