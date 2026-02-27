namespace MarketZone.ViewModels.AdminUsers
{
	public class AdminUsersPageViewModel
	{
		public string? SearchTerm { get; set; }
		public int CurrentPage { get; set; }
		public int TotalPages { get; set; }
		public List<AdminUserListItemViewModel> Users { get; set; } = new();
	}
}	