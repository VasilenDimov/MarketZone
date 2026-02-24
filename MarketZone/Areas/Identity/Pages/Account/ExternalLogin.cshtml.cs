// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MarketZone.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketZone.Areas.Identity.Pages.Account
{
	[AllowAnonymous]
	public class ExternalLoginModel : PageModel
	{
		private readonly SignInManager<User> _signInManager;
		private readonly UserManager<User> _userManager;
		private readonly IUserStore<User> _userStore;
		private readonly IUserEmailStore<User> _emailStore;
		private readonly IEmailSender _emailSender;
		private readonly ILogger<ExternalLoginModel> _logger;

		public ExternalLoginModel(
			SignInManager<User> signInManager,
			UserManager<User> userManager,
			IUserStore<User> userStore,
			ILogger<ExternalLoginModel> logger,
			IEmailSender emailSender)
		{
			_signInManager = signInManager;
			_userManager = userManager;
			_userStore = userStore;
			_emailStore = GetEmailStore();
			_logger = logger;
			_emailSender = emailSender;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public string ProviderDisplayName { get; set; }
		public string ReturnUrl { get; set; }

		[TempData]
		public string ErrorMessage { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			public string Email { get; set; }
		}

		public IActionResult OnGet() => RedirectToPage("./Login");

		public IActionResult OnPost(string provider, string returnUrl = null)
		{
			returnUrl ??= Url.Content("~/");

			var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
			var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
			return new ChallengeResult(provider, properties);
		}

		public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
		{
			returnUrl ??= Url.Content("~/");

			if (remoteError != null)
			{
				ErrorMessage = $"Error from external provider: {remoteError}";
				return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
			}

			var info = await _signInManager.GetExternalLoginInfoAsync();
			if (info == null)
			{
				ErrorMessage = "Error loading external login information.";
				return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
			}

			//) If the external login is already linked -> sign in
			var result = await _signInManager.ExternalLoginSignInAsync(
				info.LoginProvider,
				info.ProviderKey,
				isPersistent: false,
				bypassTwoFactor: true);

			if (result.Succeeded)
			{
				_logger.LogInformation("{Name} logged in with {LoginProvider} provider.",
					info.Principal.Identity?.Name, info.LoginProvider);

				return LocalRedirect(returnUrl);
			}

			if (result.IsLockedOut)
				return RedirectToPage("./Lockout");

			//  If login is linked to a user (extra safety check)
			var userByLogin = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
			if (userByLogin != null)
			{
				await _signInManager.SignInAsync(userByLogin, isPersistent: false);
				return LocalRedirect(returnUrl);
			}

			// Get email from provider
			var email = info.Principal.FindFirstValue(ClaimTypes.Email);

			// If provider did NOT give an email -> fallback to the confirmation page
			if (string.IsNullOrWhiteSpace(email))
			{
				ReturnUrl = returnUrl;
				ProviderDisplayName = info.ProviderDisplayName;
				return Page();
			}

			//  If a user already exists by email -> link Google and sign in
			var existingUser = await _userManager.FindByEmailAsync(email);
			if (existingUser != null)
			{
				if (!existingUser.EmailConfirmed)
				{
					ErrorMessage =
						"An account with this email already exists but it is not confirmed. " +
						"Please confirm your email or use 'Resend email confirmation'.";

					return RedirectToPage("./Login", new { ReturnUrl = returnUrl, email, showResend = true });
				}

				var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
				if (!addLoginResult.Succeeded)
				{
					ErrorMessage = "This Google account is already linked to another user.";
					return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
				}

				await _signInManager.SignInAsync(existingUser, isPersistent: false);
				_logger.LogInformation("Linked {LoginProvider} to existing user {UserId} and signed in.",
					info.LoginProvider, existingUser.Id);

				return LocalRedirect(returnUrl);
			}

			//auto-create + link + sign in
			var user = CreateUser();
			user.CreatedOn = DateTime.UtcNow;
			user.EmailConfirmed = true;

			await _userStore.SetUserNameAsync(user, email, CancellationToken.None);
			await _emailStore.SetEmailAsync(user, email, CancellationToken.None);

			var createResult = await _userManager.CreateAsync(user);
			if (!createResult.Succeeded)
			{
				ErrorMessage = string.Join(" ", createResult.Errors.Select(e => e.Description));
				return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
			}

			var loginResult = await _userManager.AddLoginAsync(user, info);
			if (!loginResult.Succeeded)
			{
				// Clean up created user if login linking failed
				await _userManager.DeleteAsync(user);

				ErrorMessage = string.Join(" ", loginResult.Errors.Select(e => e.Description));
				return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
			}

			_logger.LogInformation("User created an account using {LoginProvider} provider.", info.LoginProvider);

			await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
			return LocalRedirect(returnUrl);
		}

		public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
		{
			// This handler remains as a fallback ONLY for providers that don't return an email.
			returnUrl ??= Url.Content("~/");

			var info = await _signInManager.GetExternalLoginInfoAsync();
			if (info == null)
			{
				ErrorMessage = "Error loading external login information during confirmation.";
				return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
			}

			if (ModelState.IsValid)
			{
				var existing = await _userManager.FindByEmailAsync(Input.Email);
				if (existing != null)
				{
					ErrorMessage = "An account with this email already exists. Please log in.";
					return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
				}

				var user = CreateUser();
				user.CreatedOn = DateTime.UtcNow;
				user.EmailConfirmed = true;

				await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
				await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

				var createResult = await _userManager.CreateAsync(user);
				if (createResult.Succeeded)
				{
					var loginResult = await _userManager.AddLoginAsync(user, info);
					if (loginResult.Succeeded)
					{
						_logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

						await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
						return LocalRedirect(returnUrl);
					}

					foreach (var error in loginResult.Errors)
						ModelState.AddModelError(string.Empty, error.Description);
				}
				else
				{
					foreach (var error in createResult.Errors)
						ModelState.AddModelError(string.Empty, error.Description);
				}
			}

			ProviderDisplayName = info.ProviderDisplayName;
			ReturnUrl = returnUrl;
			return Page();
		}

		private User CreateUser()
		{
			try
			{
				return Activator.CreateInstance<User>();
			}
			catch
			{
				throw new InvalidOperationException($"Can't create an instance of '{nameof(User)}'. " +
					$"Ensure that '{nameof(User)}' is not an abstract class and has a parameterless constructor.");
			}
		}

		private IUserEmailStore<User> GetEmailStore()
		{
			if (!_userManager.SupportsUserEmail)
				throw new NotSupportedException("The default UI requires a user store with email support.");

			return (IUserEmailStore<User>)_userStore;
		}
	}
}