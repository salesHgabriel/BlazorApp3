using BlazorApp3.Application.Cqrs;
using BlazorApp3.Data;
using BlazorApp3.Localization;
using BlazorApp3.Security;
using BlazorApp3.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp3.Features.Account;

public sealed record SessionItem(
    string Id, string Browser, string Os, string DeviceType, string? IpAddress,
    DateTime CreatedAt, DateTime LastSeenAt, bool Persistent, bool Mfa, bool Online);

/// <summary>Sessões ativas do usuário (mais recentes primeiro).</summary>
public sealed record MySessionsQuery(string UserId) : IQuery<List<SessionItem>>;

/// <summary>
/// Encerra uma sessão do próprio usuário (<paramref name="TargetSessionId"/>) ou todas as outras (null).
/// Exige senha e, com MFA ativo, o código. Não permite encerrar a sessão atual por aqui (use "Sair").
/// </summary>
public sealed record RevokeSessionCommand(string UserId, string CurrentSessionId, string? TargetSessionId, string Password, string? Code) : ICommand<int>
{
    public override string ToString() => $"{nameof(RevokeSessionCommand)} {{ UserId = {UserId}, Target = {TargetSessionId ?? "all others"} }}"; // sem senha em logs
}

internal sealed class MySessionsHandler(AppDbContext db) : IQueryHandler<MySessionsQuery, List<SessionItem>>
{
    public Task<List<SessionItem>> HandleAsync(MySessionsQuery query, CancellationToken ct) =>
        db.UserSessions.AsNoTracking()
            .Where(s => s.UserId == query.UserId && s.RevokedAt == null)
            .OrderByDescending(s => s.LastSeenAt)
            .Select(s => new SessionItem(s.Id, s.Browser, s.Os, s.DeviceType, s.IpAddress, s.CreatedAt, s.LastSeenAt,
                s.Persistent, s.Mfa, s.Connections.Any()))
            .ToListAsync(ct);
}

internal sealed class RevokeSessionHandler(AppDbContext db, ReauthVerifier reauth, UserSessions sessions) : ICommandHandler<RevokeSessionCommand, int>
{
    public async Task<int> HandleAsync(RevokeSessionCommand command, CancellationToken ct)
    {
        if (command.TargetSessionId == command.CurrentSessionId)
            throw new AppException(AppText.Get("Para encerrar esta sessão use \"Sair\"."));
        if (command.TargetSessionId is not null &&
            !await db.UserSessions.AnyAsync(s => s.Id == command.TargetSessionId && s.UserId == command.UserId && s.RevokedAt == null, ct))
            throw new AppException(AppText.Get("Esta sessão já foi encerrada."));

        await reauth.VerifyAsync(command.UserId, command.Password, command.Code);

        return command.TargetSessionId is { } target
            ? (await sessions.RevokeAsync(target, SessionRevokeReason.Revoked, command.CurrentSessionId) ? 1 : 0)
            : await sessions.RevokeAllAsync(command.UserId, SessionRevokeReason.Revoked, exceptSessionId: command.CurrentSessionId, bySessionId: command.CurrentSessionId);
    }
}
