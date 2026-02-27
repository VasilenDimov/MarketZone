namespace MarketZone.ViewModels.AdminUsers
{
	public class AdminUserListItemViewModel
	{
		public string Id { get; set; } = null!;
		public string Email { get; set; } = null!;
		public string UserName { get; set; } = null!;
		public bool IsDeleted { get; set; }
		public DateTime? DeletedOn { get; set; }
		public DateTime CreatedOn { get; set; }
		public bool IsModerator { get; set; }
	}
}