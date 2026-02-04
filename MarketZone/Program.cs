using MarketZone.Data;
using MarketZone.Data.Models;
using MarketZone.Infrastructure.SignalR;
using MarketZone.Middlewares;
using MarketZone.Services.Implementations;
using MarketZone.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.SignalR;
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
		options.SignIn.RequireConfirmedAccount = false;
	})
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<ApplicationDbContext>();

// SignalR (ONLY ONCE)
builder.Services.AddSignalR(options =>
{
	options.EnableDetailedErrors = true;
});

// SignalR user identification
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();

// Email sender
builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();

// Background service
builder.Services.AddHostedService<EmailVerificationCleanupService>();

// MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Application services
builder.Services.AddScoped<IAdService, AdService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();
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
