using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;

namespace BlazorApp3.Features.Branding;

public sealed record SaveBrandingCommand(BrandingSettings Settings) : ICommand;
