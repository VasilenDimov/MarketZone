namespace MarketZone.ViewModels.Message
{
	public class InboxViewModel
	{
		public string Mode { get; set; } = "buying";
		public List<InboxChatItemViewModel> Chats { get; set; } = new();
	}

}
