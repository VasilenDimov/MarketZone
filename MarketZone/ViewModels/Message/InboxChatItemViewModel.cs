namespace MarketZone.ViewModels.Message
{
	public class InboxChatItemViewModel
	{
		public int AdId { get; set; }
		public string AdTitle { get; set; } = null!;
		public string OtherUserName { get; set; } = null!;
		public string LastMessage { get; set; } = null!;
		public DateTime LastMessageTime { get; set; }
	}

}
