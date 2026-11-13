namespace BlazorApp3;

/// <summary>Filas do Hangfire (a ordem em <see cref="All"/> é a prioridade de processamento).</summary>
public static class JobQueues
{
    public const string Emails = "emails";
    public const string Default = "default";
    public static readonly string[] All = [Emails, Default];
}
