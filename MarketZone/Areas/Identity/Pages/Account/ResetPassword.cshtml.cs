// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using MarketZone.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace MarketZone.Areas.Identity.Pages.Account
{
	[AllowAnonymous]
	public class ResetPasswordModel : PageModel
	{
		private readonly UserManager<User> _userManager;

		public ResetPasswordModel(UserManager<User> userManager)
		{
			_userManager = userManager;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			public string Email { get; set; }

			[Required]
			[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
			[DataType(DataType.Password)]
			public string Password { get; set; }

			[DataType(DataType.Password)]
			[Display(Name = "Confirm password")]
			[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
			public string ConfirmPassword { get; set; }

			[Required]
			public string Code { get; set; }
		}

		public IActionResult OnGet(string code = null, string email = null)
		{
			if (string.IsNullOrWhiteSpace(code))
				return BadRequest("A code must be supplied for password reset.");

			if (string.IsNullOrWhiteSpace(email))
				return BadRequest("An email must be supplied for password reset.");

			Input = new InputModel
			{
				Email = email.Trim(),
				Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
			};

			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
				return Page();

			var user = await _userManager.FindByEmailAsync(Input.Email);

			if (user == null)
			{
				ModelState.AddModelError(string.Empty, "No account found with this email.");
				return Page();
			}

			var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);

			if (result.Succeeded)
			{
				TempData["StatusMessage"] = "Your password has been reset. Please log in.";
				return RedirectToPage("./Login");
			}

			foreach (var error in result.Errors)
				ModelState.AddModelError(string.Empty, error.Description);

			return Page();
		}
	}
}