#nullable disable

using System.Text;
using MarketZone.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace MarketZone.Areas.Identity.Pages.Account
{
	public class ConfirmEmailChangeModel : PageModel
	{
		private readonly UserManager<User> _userManager;
		private readonly SignInManager<User> _signInManager;

		public ConfirmEmailChangeModel(UserManager<User> userManager, SignInManager<User> signInManager)
		{
			_userManager = userManager;
			_signInManager = signInManager;
		}

		[TempData]
		public string StatusMessage { get; set; }

		public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
		{
			if (userId == null || email == null || code == null)
			{
				return RedirectToPage("/Index");
			}

			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{userId}'.");
			}

			// Capture current values before email change
			var oldEmail = await _userManager.GetEmailAsync(user);
			var oldUserName = await _userManager.GetUserNameAsync(user);

			code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
			var result = await _userManager.ChangeEmailAsync(user, email, code);
			if (!result.Succeeded)
			{
				StatusMessage = "Error changing email.";
				return Page();
			}

			// Update username only if it was previously the same as the old email
			if (!string.IsNullOrWhiteSpace(oldEmail) &&
				!string.IsNullOrWhiteSpace(oldUserName) &&
				string.Equals(oldUserName, oldEmail, StringComparison.OrdinalIgnoreCase))
			{
				var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
				if (!setUserNameResult.Succeeded)
				{
					StatusMessage = "Error changing user name.";
					return Page();
				}
			}

			await _signInManager.RefreshSignInAsync(user);
			StatusMessage = "Thank you for confirming your email change.";
			return Page();
		}
	}
}