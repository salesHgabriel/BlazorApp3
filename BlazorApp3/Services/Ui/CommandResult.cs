namespace BlazorApp3.Services;

public readonly record struct CommandResult<T>(bool Succeeded, T? Value)
{
    public static CommandResult<T> Ok(T value) => new(true, value);
    public static readonly CommandResult<T> Failed = new(false, default);
}
