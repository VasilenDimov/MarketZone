namespace MarketZone.ViewModels.Message
{
	public class ChatViewModel
	{
		public int AdId { get; set; }
		public string ChatId { get; set; } = null!;
		public string OtherUserName { get; set; } = null!;
		public List<ChatMessageViewModel> Messages { get; set; } = new();
	}

}
