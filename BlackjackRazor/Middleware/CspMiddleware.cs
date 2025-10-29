using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BlackjackRazor.Middleware;

public class CspMiddleware
{
    private readonly RequestDelegate _next;

    // Allow remote HTTPS images + data URIs, restrict everything else fairly tightly for now.
    // 'unsafe-inline' for style temporarily until all inline styles removed; scripts not yet used.
    // Added tailwindcdn.com to script-src to permit Tailwind CDN fallback in Development.
    // NOTE: Inline styles already allowed for Tailwind's injected <style> tags via 'unsafe-inline'.
    private const string CspValue = "default-src 'self'; img-src 'self' https: data:; style-src 'self' 'unsafe-inline'; script-src 'self' https://cdn.tailwindcss.com; object-src 'none'; base-uri 'self'; frame-ancestors 'none';";

    public CspMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Only add if not already set (allow overrides/tests later)
        // Set or overwrite CSP header (idempotent). Avoid .Add to prevent exceptions if already present.
        context.Response.Headers["Content-Security-Policy"] = CspValue;
        await _next(context);
    }
}

public static class CspMiddlewareExtensions
{
    public static IApplicationBuilder UseAppCsp(this IApplicationBuilder app)
        => app.UseMiddleware<CspMiddleware>();
}
