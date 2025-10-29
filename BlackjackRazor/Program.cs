using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using BlackjackRazor.Middleware;
using BlackjackRazor.Services;
using BlackjackRazor.Data;
using Microsoft.EntityFrameworkCore;
using BlackjackRazor.Infrastructure;
using Microsoft.Extensions.Options;
using BlackjackRazor.Domain;
using BlackjackRazor.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddRazorPages(options =>
{
    // Future: authorize game pages
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/SignIn";
        options.Cookie.Name = "bj_auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });
builder.Services.AddAuthorization();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "bj_session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Simple in-memory user store until EF Core added (Task 6)
builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();

// EF Core Sqlite
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "blackjack.db");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite($"Data Source={dataPath}"));

// Typed Deck API client
builder.Services.AddHttpClient<DeckApiClient>(client =>
{
    client.BaseAddress = new Uri("https://deckofcardsapi.com");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddTransient<IDeckApiClient, DeckApiClient>();
builder.Services.Configure<BlackjackOptions>(builder.Configuration.GetSection("Blackjack"));
builder.Services.AddSingleton<IShoeManager, ShoeManager>();
builder.Services.AddSingleton<IPayoutService, PayoutService>();
builder.Services.AddSingleton<IDealerLogic, DealerLogic>();
// Game orchestrator should be scoped per request to avoid cross-user state leakage.
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddSingleton<IGameStateSerializer, GameStateSerializer>();
builder.Services.AddScoped<IGameRoundPersister, EfGameRoundPersister>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Central CSP middleware (dedicated class) ensures consistent header & future testability.
app.UseAppCsp();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
