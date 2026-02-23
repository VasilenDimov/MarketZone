// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketZone.Areas.Identity.Pages.Account
{
	[AllowAnonymous]
	public class ResetPasswordConfirmationModel : PageModel
	{
		public IActionResult OnGet()
		{
			// In case something still redirects here, go to Login with a message.
			TempData["StatusMessage"] = "Password reset successful. Please log in.";
			return RedirectToPage("./Login");
		}
	}
}