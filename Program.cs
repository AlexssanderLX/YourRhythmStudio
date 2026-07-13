using YourRhythmStudio.Infrastructure.Data;
using YourRhythmStudio.Infrastructure.Foundation;
using YourRhythmStudio.Infrastructure.Root;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Infrastructure
builder.Services.AddYourRhythmDatabase(builder.Configuration);
builder.Services.AddYourRhythmFoundation();

// Authentication
builder.Services
    .AddAuthentication("YourRhythmCookie")
    .AddCookie("YourRhythmCookie", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";

        options.Cookie.Name = "YourRhythm.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.RemoveFoundationDemoAccountsAsync();
}

await RootBootstrap.EnsureRootAccountAsync(app.Services);
await RootBootstrap.EnsureDefaultPlansAsync(app.Services);
await RootBootstrap.EnsureDefaultLandingTracksAsync(app.Services);

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles(); // serves wwwroot/uploads/ and other runtime-written files
app.MapStaticAssets();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
