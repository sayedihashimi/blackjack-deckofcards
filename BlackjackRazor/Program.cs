using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using BlackjackRazor.Data;
using BlackjackRazor.Services;
using BlackjackRazor.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    // Require auth for everything except SignIn
    options.Conventions.AllowAnonymousToPage("/SignIn");
});

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/SignIn";
        options.LogoutPath = "/SignOut";
    });
builder.Services.AddAuthorization();

builder.Services.AddSession();
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<DeckApiClient>(c =>
{
    c.BaseAddress = new Uri("https://deckofcardsapi.com");
});
builder.Services.AddScoped<BlackjackEngine>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
