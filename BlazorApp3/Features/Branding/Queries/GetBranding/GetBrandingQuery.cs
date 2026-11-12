using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;

namespace BlazorApp3.Features.Branding;

/// <summary>Whitelabel global (ou o padrão, se ainda não configurado).</summary>
public sealed record GetBrandingQuery : IQuery<BrandingSettings>;
