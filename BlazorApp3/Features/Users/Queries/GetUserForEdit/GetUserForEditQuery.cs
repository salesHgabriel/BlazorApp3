using BlazorApp3.Application.Cqrs;

namespace BlazorApp3.Features.Users;

public sealed record GetUserForEditQuery(string Id) : IQuery<UserEditModel?>;
