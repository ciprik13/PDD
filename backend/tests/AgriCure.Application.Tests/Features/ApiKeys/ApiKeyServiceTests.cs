using System.Globalization;
using AgriCure.Application.Common.ApiKeys;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Domain.Detections;
using AgriCure.Domain.Identity;
using AgriCure.Domain.Pictures;
using AgriCure.Infrastructure.ApiKeys;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgriCure.Application.Tests.Features.ApiKeys;

public sealed class ApiKeyServiceTests
{
    [Fact]
    public async Task IssueAsync_returns_plaintext_with_expected_prefix_and_length()
    {
        var (svc, db, clock) = Build("issue-prefix");

        var result = await svc.IssueAsync(Guid.NewGuid(), "device-1", Guid.NewGuid(), default);

        result.PlaintextKey.Should().StartWith("pdd_live_");
        result.PlaintextKey.Should().MatchRegex("^pdd_live_[A-Za-z0-9_-]{43}$");
        result.TokenLast4.Should().Be(result.PlaintextKey[^4..]);

        _ = db; _ = clock;
    }

    [Fact]
    public async Task IssueAsync_persists_hash_only_not_plaintext()
    {
        var (svc, db, _) = Build("issue-hash");

        var result = await svc.IssueAsync(Guid.NewGuid(), "device-1", Guid.NewGuid(), default);

        var row = await db.ApiKeys.SingleAsync(k => k.Id == result.Id);
        row.TokenHash.Should().HaveLength(64);
        row.TokenHash.Should().NotContain(result.PlaintextKey);

        var expected = ApiKeyService.Sha256Hex(result.PlaintextKey);
        row.TokenHash.Should().Be(expected);
    }

    [Fact]
    public async Task IssueAsync_writes_default_scope_and_active_state()
    {
        var (svc, db, clock) = Build("issue-defaults");

        var now = DateTimeOffset.UtcNow;
        clock.SetUtcNow(now);

        var result = await svc.IssueAsync(Guid.NewGuid(), "device-1", Guid.NewGuid(), default);

        var row = await db.ApiKeys.SingleAsync(k => k.Id == result.Id);
        row.Scope.Should().Be("detections:ingest");
        row.RevokedAt.Should().BeNull();
        row.LastUsedAt.Should().BeNull();
        row.CreatedAt.Should().Be(now);
    }

    [Fact]
    public async Task ResolveByTokenHashAsync_returns_active_key_when_hash_matches()
    {
        var (svc, _, _) = Build("resolve-hit");

        var created = await svc.IssueAsync(Guid.NewGuid(), "device-1", Guid.NewGuid(), default);
        var hash = ApiKeyService.Sha256Hex(created.PlaintextKey);

        var found = await svc.ResolveByTokenHashAsync(hash, default);

        found.Should().NotBeNull();
        found!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task ResolveByTokenHashAsync_returns_null_when_revoked()
    {
        var (svc, _, _) = Build("resolve-revoked");

        var created = await svc.IssueAsync(Guid.NewGuid(), "device-1", Guid.NewGuid(), default);
        var hash = ApiKeyService.Sha256Hex(created.PlaintextKey);

        (await svc.RevokeAsync(created.Id, default)).Should().BeTrue();

        var found = await svc.ResolveByTokenHashAsync(hash, default);
        found.Should().BeNull();
    }

    [Fact]
    public async Task ResolveByTokenHashAsync_returns_null_for_unknown_hash()
    {
        var (svc, _, _) = Build("resolve-miss");

        var found = await svc.ResolveByTokenHashAsync("0".PadLeft(64, '0'), default);

        found.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAsync_returns_false_for_unknown_id()
    {
        var (svc, _, _) = Build("revoke-unknown");

        var result = await svc.RevokeAsync(Guid.NewGuid(), default);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAsync_is_idempotent_for_already_revoked()
    {
        var (svc, db, clock) = Build("revoke-idempotent");

        var first = DateTimeOffset.Parse("2026-05-25T10:00:00Z", CultureInfo.InvariantCulture);
        clock.SetUtcNow(first);
        var created = await svc.IssueAsync(Guid.NewGuid(), "device-1", Guid.NewGuid(), default);
        (await svc.RevokeAsync(created.Id, default)).Should().BeTrue();

        var firstRevokedAt = (await db.ApiKeys.SingleAsync(k => k.Id == created.Id)).RevokedAt;

        clock.SetUtcNow(first.AddHours(1));
        (await svc.RevokeAsync(created.Id, default)).Should().BeTrue();

        var second = await db.ApiKeys.SingleAsync(k => k.Id == created.Id);
        second.RevokedAt.Should().Be(firstRevokedAt);
    }

    [Fact]
    public async Task TouchLastUsedAsync_sets_value_on_first_call()
    {
        var (svc, db, clock) = Build("touch-first");

        var t = DateTimeOffset.Parse("2026-05-25T12:00:00Z", CultureInfo.InvariantCulture);
        clock.SetUtcNow(t);
        var created = await svc.IssueAsync(Guid.NewGuid(), "device-1", Guid.NewGuid(), default);

        await svc.TouchLastUsedAsync(created.Id, default);

        (await db.ApiKeys.SingleAsync(k => k.Id == created.Id))
            .LastUsedAt.Should().Be(t);
    }

    [Fact]
    public async Task TouchLastUsedAsync_throttles_subsequent_calls_within_5_minutes()
    {
        var (svc, db, clock) = Build("touch-throttle");

        var t = DateTimeOffset.Parse("2026-05-25T12:00:00Z", CultureInfo.InvariantCulture);
        clock.SetUtcNow(t);
        var created = await svc.IssueAsync(Guid.NewGuid(), "device-1", Guid.NewGuid(), default);

        await svc.TouchLastUsedAsync(created.Id, default);
        clock.SetUtcNow(t.AddMinutes(2));
        await svc.TouchLastUsedAsync(created.Id, default);

        (await db.ApiKeys.SingleAsync(k => k.Id == created.Id))
            .LastUsedAt.Should().Be(t);
    }

    [Fact]
    public async Task TouchLastUsedAsync_updates_after_throttle_window()
    {
        var (svc, db, clock) = Build("touch-after-window");

        var t = DateTimeOffset.Parse("2026-05-25T12:00:00Z", CultureInfo.InvariantCulture);
        clock.SetUtcNow(t);
        var created = await svc.IssueAsync(Guid.NewGuid(), "device-1", Guid.NewGuid(), default);

        await svc.TouchLastUsedAsync(created.Id, default);
        var later = t.AddMinutes(6);
        clock.SetUtcNow(later);
        await svc.TouchLastUsedAsync(created.Id, default);

        (await db.ApiKeys.SingleAsync(k => k.Id == created.Id))
            .LastUsedAt.Should().Be(later);
    }

    [Fact]
    public async Task ListAsync_filters_by_owner_and_excludes_revoked_by_default()
    {
        var (svc, _, _) = Build("list-filters");

        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        var aliceActive = await svc.IssueAsync(alice, "alice-a", Guid.NewGuid(), default);
        var aliceRevoked = await svc.IssueAsync(alice, "alice-b", Guid.NewGuid(), default);
        var bobActive = await svc.IssueAsync(bob, "bob-a", Guid.NewGuid(), default);

        await svc.RevokeAsync(aliceRevoked.Id, default);

        var active = await svc.ListAsync(alice, includeRevoked: false, default);
        active.Should().ContainSingle().Which.Id.Should().Be(aliceActive.Id);

        var all = await svc.ListAsync(alice, includeRevoked: true, default);
        all.Should().HaveCount(2);
        all.Select(k => k.Id).Should().BeEquivalentTo(new[] { aliceActive.Id, aliceRevoked.Id });

        var bobOnly = await svc.ListAsync(bob, includeRevoked: false, default);
        bobOnly.Should().ContainSingle().Which.Id.Should().Be(bobActive.Id);
    }

    private static (IApiKeyService svc, ApiKeyDbContext db, TestTimeProvider clock) Build(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApiKeyDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new ApiKeyDbContext(options);
        var clock = new TestTimeProvider();
        var svc = new ApiKeyService(new TestContext(db), clock);
        return (svc, db, clock);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void SetUtcNow(DateTimeOffset value) => _now = value;
    }

    private sealed class TestContext(ApiKeyDbContext inner) : IApplicationDbContext
    {
        public DbSet<RefreshToken> RefreshTokens => inner.RefreshTokens;
        public DbSet<Detection> Detections => inner.Detections;
        public DbSet<ClassPrediction> Predictions => inner.Predictions;
        public DbSet<Plant> Plants => inner.Plants;
        public DbSet<Picture> Pictures => inner.Pictures;
        public DbSet<ApiKey> ApiKeys => inner.ApiKeys;
        public Task<int> SaveChangesAsync(CancellationToken ct) => inner.SaveChangesAsync(ct);
    }

    private sealed class ApiKeyDbContext(DbContextOptions<ApiKeyDbContext> options) : DbContext(options)
    {
        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
        public DbSet<Plant> Plants => Set<Plant>();
        public DbSet<Detection> Detections => Set<Detection>();
        public DbSet<ClassPrediction> Predictions => Set<ClassPrediction>();
        public DbSet<Picture> Pictures => Set<Picture>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<ApiKey>().Ignore(k => k.IsActive);
            builder.Entity<Detection>().OwnsOne(d => d.BoundingBox);
            builder.Entity<Detection>().HasMany(d => d.Predictions).WithOne().HasForeignKey(p => p.DetectionId);
            base.OnModelCreating(builder);
        }
    }
}
