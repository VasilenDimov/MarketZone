namespace MarketZone.ViewModels.AdminUsers
{
	public class AdminUserActionsViewModel
	{
		public string TargetUserId { get; set; } = null!;
		public bool IsDeleted { get; set; }
		public bool IsAdmin { get; set; }
		public bool IsModerator { get; set; }
	}
}