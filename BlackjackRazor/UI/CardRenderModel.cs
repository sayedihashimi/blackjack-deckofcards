namespace BlackjackRazor.UI;

public sealed record CardRenderModel(string Code, bool Hidden = false, string Size = "md", bool Highlight = false)
{
    // No longer needed: all cards use .card-resp for responsive sizing
    public string SizeClasses => string.Empty;
}
