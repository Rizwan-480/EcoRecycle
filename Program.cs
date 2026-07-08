using Microsoft.AspNetCore.Authentication.Cookies;
using EcoRecycle.DAL;
using EcoRecycle.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register DALs
builder.Services.AddScoped<DatabaseHelper>();
builder.Services.AddScoped<UserDAL>();
builder.Services.AddScoped<PickupDAL>();
builder.Services.AddScoped<RewardDAL>();
builder.Services.AddScoped<CampaignDAL>();
builder.Services.AddScoped<NotificationDAL>();
builder.Services.AddScoped<ContentDAL>();

// Register AI Waste Classifier Service
builder.Services.AddScoped<WasteClassifierService>();

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Standard static files middleware to serve css, js, and images
app.UseStaticFiles();

app.UseRouting();

// Order is critical for cookie authorization checks
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
