namespace BlazorApp3.Services;

/// <summary>Opção de seleção (MultiSelect, filtros), com cor opcional.</summary>
public sealed record SelectOption<T>(T Value, string Label, string? Color = null);
