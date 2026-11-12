using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;

namespace BlazorApp3.Features.Branding;

/// <summary>Restaura o whitelabel padrão e retorna a configuração aplicada.</summary>
public sealed record ResetBrandingCommand : ICommand<BrandingSettings>;
