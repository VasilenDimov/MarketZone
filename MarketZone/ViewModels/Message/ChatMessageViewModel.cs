namespace MarketZone.ViewModels.Message
{
	public class ChatMessageViewModel
	{
		public string SenderName { get; set; } = null!;
		public string Content { get; set; } = null!;
		public DateTime SentOn { get; set; }
	}

}
