using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Features.Detections;
using AgriCure.Domain.Detections;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgriCure.Application.Tests.Features.Detections;

public sealed class GetDetectionsQueryHandlerTests
{
    [Fact]
    public async Task Admin_sees_all_detections_across_plants()
    {
        await using var db = BuildContext("admin-sees-all");
        SeedThreeDetections(db, ownerOfA: Guid.NewGuid(), ownerOfB: Guid.NewGuid(), ownerOfC: null);
        await db.SaveChangesAsync();

        var handler = new GetDetectionsQueryHandler(
            new TestContext(db),
            new FakeCurrentUser(isAdmin: true));

        var result = await handler.Handle(new GetDetectionsQuery(50), default);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Agriculture_sees_only_detections_for_plants_they_own()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();

        await using var db = BuildContext("agriculture-scoped");
        SeedThreeDetections(db, ownerOfA: alice, ownerOfB: bob, ownerOfC: null);
        await db.SaveChangesAsync();

        var handler = new GetDetectionsQueryHandler(
            new TestContext(db),
            new FakeCurrentUser(isAdmin: false, userId: alice));

        var result = await handler.Handle(new GetDetectionsQuery(50), default);

        result.Should().HaveCount(1);
        result[0].PlantId.Should().Be("plant-A");
    }

    private static AppDbContextForTests BuildContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContextForTests>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContextForTests(options);
    }

    private static void SeedThreeDetections(
        AppDbContextForTests db,
        Guid ownerOfA,
        Guid ownerOfB,
        Guid? ownerOfC)
    {
        db.Plants.Add(new Plant { Id = "plant-A", OwnerUserId = ownerOfA, CreatedAt = DateTimeOffset.UtcNow });
        db.Plants.Add(new Plant { Id = "plant-B", OwnerUserId = ownerOfB, CreatedAt = DateTimeOffset.UtcNow });
        db.Plants.Add(new Plant { Id = "plant-C", OwnerUserId = ownerOfC, CreatedAt = DateTimeOffset.UtcNow });

        db.Detections.Add(BuildDetection("plant-A"));
        db.Detections.Add(BuildDetection("plant-B"));
        db.Detections.Add(BuildDetection("plant-C"));
    }

    private static Detection BuildDetection(string plantId) => new()
    {
        Id = Guid.NewGuid(),
        FrameId = 1,
        Timestamp = DateTimeOffset.UtcNow,
        Severity = Severity.Warning,
        Predictions = new List<ClassPrediction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                DiseaseClass = DiseaseClass.EarlyBlight,
                Confidence = 0.9,
                Label = "Early Blight",
                Rank = 0,
            },
        },
        BoundingBox = new BoundingBox(0.1, 0.2, 0.3, 0.4, 0.5, 10.0),
        InferenceMs = 25,
        ConfidenceGatePassed = true,
        Row = 1,
        PlantId = plantId,
        PositionMeters = 5.0,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeCurrentUser : ICurrentUserAccessor
    {
        private readonly Guid? _userId;

        public FakeCurrentUser(bool isAdmin, Guid? userId = null)
        {
            IsAdmin = isAdmin;
            _userId = userId;
        }

        public Guid? UserId => _userId;
        public bool IsAuthenticated => _userId is not null;
        public bool IsAdmin { get; }
        public bool IsAgriculture => !IsAdmin && _userId is not null;
        public bool IsSystem => false;
        public Guid RequireUserId() => _userId ?? throw new InvalidOperationException("no user");
    }

    private sealed class TestContext(AppDbContextForTests inner) : IApplicationDbContext
    {
        public DbSet<AgriCure.Domain.Identity.RefreshToken> RefreshTokens => inner.RefreshTokens;
        public DbSet<Detection> Detections => inner.Detections;
        public DbSet<ClassPrediction> Predictions => inner.Predictions;
        public DbSet<Plant> Plants => inner.Plants;
        public DbSet<AgriCure.Domain.Pictures.Picture> Pictures => inner.Pictures;
        public DbSet<AgriCure.Domain.Identity.ApiKey> ApiKeys => inner.ApiKeys;
        public Task<int> SaveChangesAsync(CancellationToken ct) => inner.SaveChangesAsync(ct);
    }

    private sealed class AppDbContextForTests(DbContextOptions<AppDbContextForTests> options) : DbContext(options)
    {
        public DbSet<Plant> Plants => Set<Plant>();
        public DbSet<Detection> Detections => Set<Detection>();
        public DbSet<ClassPrediction> Predictions => Set<ClassPrediction>();
        public DbSet<AgriCure.Domain.Pictures.Picture> Pictures => Set<AgriCure.Domain.Pictures.Picture>();
        public DbSet<AgriCure.Domain.Identity.RefreshToken> RefreshTokens => Set<AgriCure.Domain.Identity.RefreshToken>();
        public DbSet<AgriCure.Domain.Identity.ApiKey> ApiKeys => Set<AgriCure.Domain.Identity.ApiKey>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Detection>().OwnsOne(d => d.BoundingBox);
            builder.Entity<Detection>().HasMany(d => d.Predictions).WithOne().HasForeignKey(p => p.DetectionId);
            base.OnModelCreating(builder);
        }
    }
}
