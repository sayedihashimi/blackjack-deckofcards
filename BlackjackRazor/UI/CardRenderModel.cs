namespace BlackjackRazor.UI;

public sealed record CardRenderModel(string Code, bool Hidden = false, string Size = "md", bool Highlight = false)
{
    public string SizeClasses => Size switch
    {
        "sm" => "w-10 h-14 text-xs",
        "lg" => "w-16 h-24 text-base",
        _ => "w-12 h-16 text-sm"
    };
}
