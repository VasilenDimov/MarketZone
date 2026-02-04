// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable
using MarketZone.Data;
using MarketZone.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace MarketZone.Areas.Identity.Pages.Account
{
	public class RegisterModel : PageModel
	{
		private readonly SignInManager<User> _signInManager;
		private readonly UserManager<User> _userManager;
		private readonly IUserStore<User> _userStore;
		private readonly IUserEmailStore<User> _emailStore;
		private readonly ILogger<RegisterModel> _logger;
		private readonly IEmailSender _emailSender;
		private readonly ApplicationDbContext _context;

		public RegisterModel(
			UserManager<User> userManager,
			IUserStore<User> userStore,
			SignInManager<User> signInManager,
			ILogger<RegisterModel> logger,
			IEmailSender emailSender,
			ApplicationDbContext context)
		{
			_userManager = userManager;
			_userStore = userStore;
			_emailStore = GetEmailStore();
			_signInManager = signInManager;
			_logger = logger;
			_emailSender = emailSender;
			_context = context;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public IList<AuthenticationScheme> ExternalLogins { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			[Display(Name = "Email")]
			public string Email { get; set; }

			[Required]
			[StringLength(100, MinimumLength = 6)]
			[DataType(DataType.Password)]
			[Display(Name = "Password")]
			public string Password { get; set; }

			[DataType(DataType.Password)]
			[Display(Name = "Confirm password")]
			[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
			public string ConfirmPassword { get; set; }
		}

		public async Task OnGetAsync()
		{
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			if (!ModelState.IsValid)
			{
				return Page();
			}

			var user = CreateUser();
			user.CreatedOn = DateTime.UtcNow;

			await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
			await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

			var result = await _userManager.CreateAsync(user, Input.Password);

			if (!result.Succeeded)
			{
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}

				return Page();
			}

			_logger.LogInformation("User created a new account with password.");

			// 🔐 Generate 6-digit verification code
			var verificationCode = RandomNumberGenerator.GetInt32(100000, 1000000)
			.ToString();

			_context.EmailVerificationCodes.Add(new EmailVerificationCode
			{
				UserId = user.Id,
				Code = verificationCode,
				CreatedAt = DateTime.UtcNow,
				ExpiresAt = DateTime.UtcNow.AddMinutes(10)
			});

			await _context.SaveChangesAsync();

			// ✉️ Send email with code
			await _emailSender.SendEmailAsync(
				Input.Email,
				"MarketZone – verification code",
				$"Your verification code is <b>{verificationCode}</b>. It expires in 10 minutes."
			);

			// ❗ Do NOT sign in the user
			// ❗ Do NOT confirm email yet

			// ➜ Redirect to code verification page
			return RedirectToPage("VerifyEmailCode", new { email = Input.Email });
		}

		private User CreateUser()
		{
			try
			{
				return Activator.CreateInstance<User>();
			}
			catch
			{
				throw new InvalidOperationException(
					$"Can't create an instance of '{nameof(User)}'. " +
					$"Ensure that '{nameof(User)}' is not abstract and has a parameterless constructor."
				);
			}
		}

		private IUserEmailStore<User> GetEmailStore()
		{
			if (!_userManager.SupportsUserEmail)
			{
				throw new NotSupportedException("The default UI requires a user store with email support.");
			}

			return (IUserEmailStore<User>)_userStore;
		}
	}
}
