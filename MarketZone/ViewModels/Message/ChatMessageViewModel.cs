namespace MarketZone.ViewModels.Message
{
	public class ChatMessageViewModel
	{
		public string SenderId { get; set; } = null!;
		public string SenderName { get; set; } = null!;
		public string SenderProfileImage { get; set; } = "/images/default-avatar.png";
		public string Content { get; set; } = null!;
		public DateTime SentOn { get; set; }
		public List<string> ImageUrls { get; set; } = new();
	}


}
