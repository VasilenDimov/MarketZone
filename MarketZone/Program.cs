using MarketZone.Data;
using MarketZone.Data.Models;
using MarketZone.Infrastructure.Identity;
using MarketZone.Infrastructure.SignalR;
using MarketZone.Middlewares;
using MarketZone.Services.Implementations;
using MarketZone.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services
	.AddDefaultIdentity<User>(options =>
	{
		options.SignIn.RequireConfirmedAccount = true;
		options.User.RequireUniqueEmail = true;
	})
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultTokenProviders();

// External login
builder.Services.AddAuthentication()
	.AddGoogle(options =>
	{
		options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
		options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

		options.Events = new OAuthEvents
		{
			OnRedirectToAuthorizationEndpoint = context =>
			{
				var uri = new Uri(context.RedirectUri);
				var query = QueryHelpers.ParseQuery(uri.Query)
					.ToDictionary(
						kvp => kvp.Key,
						kvp => (string?)kvp.Value.ToString()
					);

				query["prompt"] = "select_account";

				var newUrl = QueryHelpers.AddQueryString(
					$"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}",
					query);

				context.Response.Redirect(newUrl);
				return Task.CompletedTask;
			}
		};
	});

// SignalR
builder.Services.AddSignalR(options =>
{
	options.EnableDetailedErrors = true;
});
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();

// Email sender
builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();

// MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Application services
builder.Services.AddScoped<IAdService, AdService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICategoryHierarchyService, CategoryHierarchyService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, AppUserClaimsPrincipalFactory>();

var app = builder.Build();

// Seed roles
using (var scope = app.Services.CreateScope())
{
	var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

	string[] roles = { "Admin", "Moderator" };

	foreach (var role in roles)
	{
		if (!await roleManager.RoleExistsAsync(role))
		{
			await roleManager.CreateAsync(new IdentityRole(role));
		}
	}
}

// Global exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<UserOnlineMiddleware>();

// SignalR Hub
app.MapHub<ChatHub>("/chatHub");

// MVC routes
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();