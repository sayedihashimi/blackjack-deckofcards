using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/SignIn";
        options.LogoutPath = "/Auth/SignOut";
    });

builder.Services.AddHttpClient<BlackjackRazor.Services.DeckApiClient>(c =>
{
    c.BaseAddress = new Uri("https://deckofcardsapi.com/");
    c.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddSingleton<BlackjackRazor.Models.IGameSessionStore, BlackjackRazor.Models.InMemoryGameSessionStore>();

builder.Services.AddDbContext<BlackjackRazor.Data.AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=blackjack.db"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
