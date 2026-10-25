using System.Security.Cryptography;
using System.Text;
using FinanceManager.Application.Abstractions;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Identity;

public sealed class RefreshTokenService(
    FinanceManagerDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ITokenGenerator tokenGenerator) : IRefreshTokenService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    public async Task<TokenPair> IssueTokensAsync(
        Guid userId,
        Guid householdId,
        HouseholdRole role,
        CancellationToken cancellationToken)
    {
        var accessToken = tokenGenerator.GenerateAccessToken(userId, householdId, role);
        var (rawToken, hash) = GenerateRawTokenAndHash();
        var now = DateTime.UtcNow;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseholdId = householdId,
            Role = role,
            TokenHash = hash,
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime),
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new TokenPair(accessToken, rawToken);
    }

    public async Task<TokenPair?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = ComputeHash(refreshToken);
        var now = DateTime.UtcNow;

        var record = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (record is null)
        {
            return null;
        }

        if (record.RevokedAt is not null)
        {
            // Already rotated or revoked before - presenting it again is a replay. Kill
            // every other active token for this user, not just the replayed one.
            await RevokeAllActiveAsync(record.UserId, now, cancellationToken);
            return null;
        }

        if (record.ExpiresAt <= now)
        {
            return null;
        }

        var userExists = await userManager.Users.AnyAsync(u => u.Id == record.UserId, cancellationToken);
        if (!userExists)
        {
            return null;
        }

        // Carried forward unchanged from the token being rotated - refresh never re-derives
        // "current" household/role from live membership state, so a role/household change
        // only takes effect on the next explicit login or household switch.
        var accessToken = tokenGenerator.GenerateAccessToken(record.UserId, record.HouseholdId, record.Role);
        var (rawToken, newHash) = GenerateRawTokenAndHash();

        // Writing RevokedAt is the atomic claim: EF includes the original (null) value in
        // the UPDATE's WHERE clause via the concurrency token, so a concurrent claim of
        // this same row throws DbUpdateConcurrencyException instead of both succeeding.
        record.RevokedAt = now;
        record.ReplacedByTokenHash = newHash;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = record.UserId,
            HouseholdId = record.HouseholdId,
            Role = record.Role,
            TokenHash = newHash,
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime),
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else claimed this token first.
            return null;
        }

        return new TokenPair(accessToken, rawToken);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = ComputeHash(refreshToken);

        var record = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (record is not { RevokedAt: null })
        {
            return;
        }

        record.RevokedAt = DateTime.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Already revoked/rotated by someone else in the meantime - fine, that's
            // what we wanted anyway.
        }
    }

    private async Task RevokeAllActiveAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static (string RawToken, string Hash) GenerateRawTokenAndHash()
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return (rawToken, ComputeHash(rawToken));
    }

    public static string ComputeHash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
