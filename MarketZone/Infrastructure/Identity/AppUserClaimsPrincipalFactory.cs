using System.Security.Claims;
using MarketZone.Common;
using MarketZone.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MarketZone.Infrastructure.Identity
{
	public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, IdentityRole>
	{
		public const string ProfilePictureUrlClaimType = "profile_picture_url";

		public AppUserClaimsPrincipalFactory(
			UserManager<User> userManager,
			RoleManager<IdentityRole> roleManager,
			IOptions<IdentityOptions> optionsAccessor)
			: base(userManager, roleManager, optionsAccessor)
		{
		}

		protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
		{
			var identity = await base.GenerateClaimsAsync(user);

			var avatarUrl = string.IsNullOrWhiteSpace(user.ProfilePictureUrl)
				? AppConstants.DefaultAvatarUrl
				: user.ProfilePictureUrl;

			var existing = identity.FindFirst(ProfilePictureUrlClaimType);
			if (existing != null)
				identity.RemoveClaim(existing);

			identity.AddClaim(new Claim(ProfilePictureUrlClaimType, avatarUrl));

			return identity;
		}
	}
}