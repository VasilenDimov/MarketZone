using MarketZone.Common;
using MarketZone.Data;
using MarketZone.Data.Enums;
using MarketZone.Data.Models;
using MarketZone.Services.Interfaces;
using MarketZone.ViewModels.Ad;
using MarketZone.ViewModels.AdminUsers;
using MarketZone.ViewModels.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarketZone.Services.Implementations
{
	public class UserService : IUserService
	{
		private readonly ApplicationDbContext context;
		private readonly IReviewService reviewService;
		private readonly ICategoryHierarchyService categoryHierarchyService;
		private readonly UserManager<User> userManager;
		private readonly RoleManager<IdentityRole> roleManager;

		public UserService(
			ApplicationDbContext context,
			IReviewService reviewService,
			ICategoryHierarchyService categoryHierarchyService,
			UserManager<User> userManager,
			RoleManager<IdentityRole> roleManager)
		{
			this.context = context;
			this.reviewService = reviewService;
			this.categoryHierarchyService = categoryHierarchyService;
			this.userManager = userManager;
			this.roleManager = roleManager;
		}

		public async Task<UserProfileViewModel> GetProfileAsync(
			string userId,
			string? search,
			string? address,
			int? categoryId,
			decimal? minPrice,
			decimal? maxPrice,
			string? tags,
			string? sort,
			int page,
			string? viewerId)
		{
			const int PageSize = 21;

			var user = await context.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(u => u.Id == userId);

			if (user == null)
				throw new ArgumentException("User not found");

			var roleUser = await userManager.FindByIdAsync(userId);
			bool isAdmin = false;
			bool isModerator = false;

			if (roleUser != null)
			{
				isAdmin = await userManager.IsInRoleAsync(roleUser, "Admin");
				isModerator = await userManager.IsInRoleAsync(roleUser, "Moderator");
			}

			var adsQuery = context.Ads
				.AsNoTracking()
				.Where(a => a.UserId == userId)
				.AsQueryable();

			var isOwner = !string.IsNullOrWhiteSpace(viewerId) && viewerId == userId;
			if (!isOwner)
			{
				adsQuery = adsQuery.Where(a => a.Status == AdStatus.Approved);
			}

			if (!string.IsNullOrWhiteSpace(search))
			{
				adsQuery = adsQuery.Where(a =>
					a.Title.Contains(search) ||
					a.Description.Contains(search));
			}

			if (!string.IsNullOrWhiteSpace(address))
			{
				var tokens = address
					.ToLower()
					.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
					.Distinct()
					.ToList();

				foreach (var t in tokens)
				{
					var token = t;
					adsQuery = adsQuery.Where(a => a.Address.ToLower().Contains(token));
				}
			}

			if (categoryId.HasValue)
			{
				var allowedCategoryIds = await categoryHierarchyService
					.GetDescendantCategoryIdsAsync(categoryId.Value);

				adsQuery = adsQuery.Where(a => allowedCategoryIds.Contains(a.CategoryId));
			}

			if (minPrice.HasValue)
				adsQuery = adsQuery.Where(a => a.Price >= minPrice.Value);

			if (maxPrice.HasValue)
				adsQuery = adsQuery.Where(a => a.Price <= maxPrice.Value);

			if (!string.IsNullOrWhiteSpace(tags))
			{
				var tagList = tags
					.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(t => t.Trim().ToLower())
					.Where(t => !string.IsNullOrWhiteSpace(t))
					.Distinct()
					.Take(10)
					.ToList();

				if (tagList.Count > 0)
				{
					adsQuery = adsQuery.Where(a =>
						a.AdTags.Any(at => tagList.Contains(at.Tag.Name.ToLower())));
				}
			}

			adsQuery = sort switch
			{
				"price_asc" => adsQuery.OrderBy(a => a.Price),
				"price_desc" => adsQuery.OrderByDescending(a => a.Price),
				"oldest" => adsQuery.OrderBy(a => a.CreatedOn),
				_ => adsQuery.OrderByDescending(a => a.CreatedOn)
			};

			var totalAds = await adsQuery.CountAsync();

			var ads = await adsQuery
				.Skip((page - 1) * PageSize)
				.Take(PageSize)
				.Select(a => new AdListItemViewModel
				{
					Id = a.Id,
					Title = a.Title,
					Price = a.Price,
					Currency = Currency.EUR,
					CreatedOn = a.CreatedOn,
					MainImageUrl = a.Images
						.OrderBy(i => i.Id)
						.Select(i => i.ImageUrl)
						.FirstOrDefault()
				})
				.ToListAsync();

			var reviews = await reviewService.GetReviewsForUserAsync(userId);

			var reviewStats = await context.Reviews
				.AsNoTracking()
				.Where(r => r.ReviewedUserId == userId)
				.GroupBy(r => r.ReviewedUserId)
				.Select(g => new
				{
					AverageRating = g.Average(r => (double?)r.Rating) ?? 0,
					ReviewCount = g.Count()
				})
				.FirstOrDefaultAsync();

			bool canReview = false;
			if (!string.IsNullOrWhiteSpace(viewerId))
				canReview = await reviewService.CanReviewAsync(viewerId, userId);

			string? categoryName = null;
			if (categoryId.HasValue)
			{
				categoryName = await context.Categories
					.AsNoTracking()
					.Where(c => c.Id == categoryId.Value)
					.Select(c => c.Name)
					.FirstOrDefaultAsync();
			}

			var displayName = user.IsDeleted ? "Deleted user" : user.UserName!;
			var profilePic = string.IsNullOrWhiteSpace(user.ProfilePictureUrl)
				? AppConstants.DefaultAvatarUrl
				: user.ProfilePictureUrl;

			return new UserProfileViewModel
			{
				UserId = user.Id,
				UserName = displayName,
				Email = user.IsDeleted ? string.Empty : (user.Email ?? string.Empty),
				ProfilePictureUrl = profilePic,
				CreatedOn = user.CreatedOn,
				LastOnlineOn = user.LastOnlineOn,

				IsDeleted = user.IsDeleted,
				IsAdmin = isAdmin,
				IsModerator = isModerator,

				SearchTerm = search,
				Address = address,
				CategoryId = categoryId,
				CategoryName = categoryName,
				MinPrice = minPrice,
				MaxPrice = maxPrice,
				Tags = tags,
				Sort = sort,

				CurrentPage = page,
				TotalPages = (int)Math.Ceiling(totalAds / (double)PageSize),

				Ads = ads,
				Reviews = reviews,
				AverageRating = reviewStats?.AverageRating ?? 0,
				ReviewCount = reviewStats?.ReviewCount ?? 0,
				CanReview = canReview
			};
		}

		public async Task<bool> SoftDeleteUserAsync(string targetUserId, string adminId)
		{
			// Prevent admin from deleting themselves from this action
			if (targetUserId == adminId)
				return false;

			var user = await userManager.FindByIdAsync(targetUserId);
			if (user == null)
				return false;

			// Prevent deleting other admins
			if (await userManager.IsInRoleAsync(user, "Admin"))
				return false;

			if (user.IsDeleted)
				return true;

			// Mark as deleted
			user.IsDeleted = true;
			user.DeletedOn = DateTime.UtcNow;

			// Lock account permanently
			await userManager.SetLockoutEnabledAsync(user, true);
			await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

			// Remove external logins (Google etc.)
			var logins = await userManager.GetLoginsAsync(user);
			foreach (var login in logins)
			{
				await userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
			}

			// Clear sensitive data (same logic as self-delete)
			user.ProfilePictureUrl = null;
			user.PhoneNumber = null;

			// Scrub email & username so they can be reused
			var token = Guid.NewGuid().ToString("N");

			user.Email = $"deleted_{token}@deleted.local";
			user.NormalizedEmail = user.Email.ToUpperInvariant();

			user.UserName = $"deleted_{token}";
			user.NormalizedUserName = user.UserName.ToUpperInvariant();

			await userManager.UpdateSecurityStampAsync(user);

			var result = await userManager.UpdateAsync(user);
			return result.Succeeded;
		}

		public async Task<AdminUsersPageViewModel> GetAdminUsersPageAsync(string? search, int page, string adminId)
		{
			const int PageSize = 20;
			if (page < 1) page = 1;

			var adminRoleId = await context.Roles
				.Where(r => r.Name == "Admin")
				.Select(r => r.Id)
				.FirstOrDefaultAsync();

			var moderatorRoleId = await context.Roles
				.Where(r => r.Name == "Moderator")
				.Select(r => r.Id)
				.FirstOrDefaultAsync();

			var adminUserIds = await context.UserRoles
				.Where(ur => ur.RoleId == adminRoleId)
				.Select(ur => ur.UserId)
				.ToListAsync();

			var moderatorUserIds = await context.UserRoles
				.Where(ur => ur.RoleId == moderatorRoleId)
				.Select(ur => ur.UserId)
				.ToListAsync();

			var query = context.Users
				.AsNoTracking()
				.Where(u => u.Id != adminId)              // hide current admin
				.Where(u => !adminUserIds.Contains(u.Id)) // hide other admins
				.Where(u => !u.IsDeleted)                 // hide deleted users
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(search))
			{
				var term = search.Trim().ToLower();
				query = query.Where(u =>
					(u.Email != null && u.Email.ToLower().Contains(term)) ||
					(u.UserName != null && u.UserName.ToLower().Contains(term)));
			}

			var totalCount = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
			if (totalPages < 1) totalPages = 1;
			if (page > totalPages) page = totalPages;

			var users = await query
				.OrderByDescending(u => u.CreatedOn)
				.Skip((page - 1) * PageSize)
				.Take(PageSize)
				.Select(u => new AdminUserListItemViewModel
				{
					Id = u.Id,
					Email = u.Email ?? string.Empty,
					UserName = u.UserName ?? string.Empty,
					IsDeleted = u.IsDeleted,
					DeletedOn = u.DeletedOn,
					CreatedOn = u.CreatedOn,
					IsModerator = moderatorUserIds.Contains(u.Id)
				})
				.ToListAsync();

			return new AdminUsersPageViewModel
			{
				SearchTerm = search,
				CurrentPage = page,
				TotalPages = totalPages,
				Users = users
			};
		}

		public async Task<bool> PromoteToModeratorAsync(string targetUserId, string adminId)
		{
			if (targetUserId == adminId)
				return false;

			var user = await userManager.FindByIdAsync(targetUserId);
			if (user == null || user.IsDeleted)
				return false;

			// cannot promote admins
			if (await userManager.IsInRoleAsync(user, "Admin"))
				return false;

			if (await userManager.IsInRoleAsync(user, "Moderator"))
				return true;

			if (!await roleManager.RoleExistsAsync("Moderator"))
			{
				var roleResult = await roleManager.CreateAsync(new IdentityRole("Moderator"));
				if (!roleResult.Succeeded)
					return false;
			}

			var result = await userManager.AddToRoleAsync(user, "Moderator");
			return result.Succeeded;
		}
		public async Task<bool> DemoteFromModeratorAsync(string targetUserId, string adminId)
		{
			if (targetUserId == adminId)
				return false;

			var user = await userManager.FindByIdAsync(targetUserId);
			if (user == null || user.IsDeleted)
				return false;

			// Cannot demote admins
			if (await userManager.IsInRoleAsync(user, "Admin"))
				return false;

			// If not a moderator, nothing to do
			if (!await userManager.IsInRoleAsync(user, "Moderator"))
				return true;

			var result = await userManager.RemoveFromRoleAsync(user, "Moderator");
			return result.Succeeded;
		}
	}
}