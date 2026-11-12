namespace BlazorApp3.Localization;

/// <summary>Idioma disponível no seletor, com a moeda padrão (ISO 4217) usada quando nenhuma é informada.</summary>
public sealed record AppCulture(string Name, string DisplayName, string Short, string Currency);
