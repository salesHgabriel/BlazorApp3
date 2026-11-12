namespace BlazorApp3.Services;

public sealed class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public ToastLevel Level { get; init; }
    public string Title { get; init; } = "";
    public string? Message { get; init; }
    public int DurationMs { get; init; }
    public bool Leaving { get; set; }
}
