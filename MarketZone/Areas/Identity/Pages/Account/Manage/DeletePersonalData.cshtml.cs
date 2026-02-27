#nullable disable

using System.ComponentModel.DataAnnotations;
using MarketZone.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketZone.Areas.Identity.Pages.Account.Manage
{
	public class DeletePersonalDataModel : PageModel
	{
		private const string ExternalDeleteProvider = "Google";
		private const string DefaultAvatarUrl = "/images/default-avatar.png";

		private readonly UserManager<User> _userManager;
		private readonly SignInManager<User> _signInManager;
		private readonly ILogger<DeletePersonalDataModel> _logger;

		public DeletePersonalDataModel(
			UserManager<User> userManager,
			SignInManager<User> signInManager,
			ILogger<DeletePersonalDataModel> logger)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_logger = logger;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public class InputModel
		{
			[DataType(DataType.Password)]
			public string Password { get; set; }
		}

		public bool RequirePassword { get; set; }

		public async Task<IActionResult> OnGet()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

			RequirePassword = await _userManager.HasPasswordAsync(user);
			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

			RequirePassword = await _userManager.HasPasswordAsync(user);

			if (!RequirePassword)
			{
				return RedirectToPage();
			}

			if (!await _userManager.CheckPasswordAsync(user, Input.Password))
			{
				ModelState.AddModelError(string.Empty, "Incorrect password.");
				return Page();
			}

			var userId = await _userManager.GetUserIdAsync(user);

			var result = await SoftDeleteUserAsync(user);
			if (!result.Succeeded)
				throw new InvalidOperationException("Unexpected error occurred deleting user.");

			await _signInManager.SignOutAsync();

			_logger.LogInformation("User with ID '{UserId}' deleted themselves.", userId);

			return Redirect("~/");
		}

		public async Task<IActionResult> OnPostStartExternalDeleteAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

			RequirePassword = await _userManager.HasPasswordAsync(user);

			if (RequirePassword)
				return RedirectToPage();

			var redirectUrl = Url.Page(
				"/Account/Manage/DeletePersonalData",
				pageHandler: "ExternalDeleteCallback",
				values: null);

			var properties = _signInManager.ConfigureExternalAuthenticationProperties(
				ExternalDeleteProvider,
				redirectUrl);

			return new ChallengeResult(ExternalDeleteProvider, properties);
		}

		public async Task<IActionResult> OnGetExternalDeleteCallbackAsync(string remoteError = null)
		{
			if (remoteError != null)
			{
				ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
				await ReloadRequirePasswordAsync();
				return Page();
			}

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

			RequirePassword = await _userManager.HasPasswordAsync(user);

			if (RequirePassword)
				return RedirectToPage();

			var info = await _signInManager.GetExternalLoginInfoAsync();
			if (info == null)
			{
				ModelState.AddModelError(string.Empty, "Error loading external login information.");
				await ReloadRequirePasswordAsync();
				return Page();
			}

			if (!string.Equals(info.LoginProvider, ExternalDeleteProvider, StringComparison.OrdinalIgnoreCase))
			{
				ModelState.AddModelError(string.Empty, "Invalid external provider.");
				await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
				await ReloadRequirePasswordAsync();
				return Page();
			}

			var linkedLogins = await _userManager.GetLoginsAsync(user);
			bool isLinked = linkedLogins.Any(l =>
				l.LoginProvider.Equals(info.LoginProvider, StringComparison.OrdinalIgnoreCase) &&
				l.ProviderKey == info.ProviderKey);

			await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

			if (!isLinked)
			{
				ModelState.AddModelError(string.Empty, "This Google account is not linked to your profile.");
				await ReloadRequirePasswordAsync();
				return Page();
			}

			var userId = await _userManager.GetUserIdAsync(user);

			var result = await SoftDeleteUserAsync(user);
			if (!result.Succeeded)
				throw new InvalidOperationException("Unexpected error occurred deleting user.");

			await _signInManager.SignOutAsync();

			_logger.LogInformation("User with ID '{UserId}' deleted themselves (external re-auth).", userId);

			return Redirect("~/");
		}

		private async Task ReloadRequirePasswordAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null) return;

			RequirePassword = await _userManager.HasPasswordAsync(user);
		}

		private async Task<IdentityResult> SoftDeleteUserAsync(User user)
		{
			if (user.IsDeleted)
				return IdentityResult.Success;

			user.IsDeleted = true;
			user.DeletedOn = DateTime.UtcNow;

			user.ProfilePictureUrl = DefaultAvatarUrl;

			user.PhoneNumber = null;
			user.PhoneNumberConfirmed = false;

			user.TwoFactorEnabled = false;
			user.EmailConfirmed = false;

			user.LockoutEnabled = true;
			user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);

			var userId = await _userManager.GetUserIdAsync(user);
			var deletedUserName = $"deleted_{userId}";
			var deletedEmail = $"deleted_{userId}@deleted.local";

			var setUserName = await _userManager.SetUserNameAsync(user, deletedUserName);
			if (!setUserName.Succeeded) return setUserName;

			var setEmail = await _userManager.SetEmailAsync(user, deletedEmail);
			if (!setEmail.Succeeded) return setEmail;

			user.PasswordHash = null;

			var logins = await _userManager.GetLoginsAsync(user);
			foreach (var login in logins)
			{
				var removeLoginResult = await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
				if (!removeLoginResult.Succeeded) return removeLoginResult;
			}

			await _userManager.UpdateSecurityStampAsync(user);

			return await _userManager.UpdateAsync(user);
		}
	}
}