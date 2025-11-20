namespace DocBuilder.Domain.Context;

public class RequestContext
{
    private static readonly AsyncLocal<string?> _trackId = new();

    public static string TrackId
    {
        get => _trackId.Value ?? string.Empty;
        set => _trackId.Value = value;
    }

    public static void Clear() => _trackId.Value = null;
}
