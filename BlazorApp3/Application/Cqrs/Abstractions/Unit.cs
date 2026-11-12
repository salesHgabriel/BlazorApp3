namespace BlazorApp3.Application.Cqrs;

/// <summary>"Sem valor" para commands que não retornam nada.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = new();
}
