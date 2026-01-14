using MarketZone.Data;
using MarketZone.Data.Models;
using MarketZone.Services.Implementations;
using MarketZone.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(
		builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services
	.AddDefaultIdentity<User>(options =>
	{
		// We control email confirmation manually
		options.SignIn.RequireConfirmedAccount = false;
	})
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<ApplicationDbContext>();

// Email sender (IMPORTANT)
builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();
// Background service for cleaning up expired email verification codes
builder.Services.AddHostedService<EmailVerificationCleanupService>();

// MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Application services
builder.Services.AddScoped<IAdService, AdService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
//Message service for SignalR
builder.Services.AddSignalR();
builder.Services.AddScoped<IMessageService, MessageService>();

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapHub<ChatHub>("/chatHub");

// Routing
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
