using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.DTOs;
using AgriCure.Application.Common.Exceptions;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Common.Pictures;
using AgriCure.Application.Features.Detections;
using AgriCure.Domain.Detections;
using AgriCure.Domain.Identity;
using AgriCure.Domain.Pictures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgriCure.Application.Tests.Features.Detections;

public sealed class IngestDetectionCommandHandlerTests
{
    [Fact]
    public async Task Returns_existing_detection_when_PlantId_FrameId_pair_already_exists()
    {
        await using var db = BuildContext("dedup-hit");
        var plantId = "plant-A";
        var frameId = 99L;

        var existingPlant = new Plant { Id = plantId, OwnerUserId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        var existingDetection = BuildDetection(plantId, frameId);
        var existingPicture = new Picture
        {
            Id = Guid.NewGuid(),
            MimeType = "image/png",
            VirtualPath = "pictures/existing.png",
            IsNew = false,
        };
        db.Plants.Add(existingPlant);
        db.Detections.Add(existingDetection);
        db.Pictures.Add(existingPicture);
        db.DetectionPictures.Add(new DetectionPicture
        {
            DetectionId = existingDetection.Id,
            PictureId = existingPicture.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var pictureService = new RecordingPictureService();
        var handler = new IngestDetectionCommandHandler(
            new TestContext(db),
            pictureService,
            new FakeCurrentUser(existingPlant.OwnerUserId!.Value),
            TimeProvider.System,
            NullLogger<IngestDetectionCommandHandler>.Instance);

        var result = await handler.Handle(
            BuildCommand(plantId, frameId),
            default);

        result.IsDuplicate.Should().BeTrue();
        result.IsNewPlant.Should().BeFalse();
        result.DetectionId.Should().Be(existingDetection.Id);
        result.PictureId.Should().Be(existingPicture.Id);
        pictureService.InsertCalls.Should().Be(0,
            because: "dedup path must short-circuit before uploading the image");
    }

    [Fact]
    public async Task Creates_plant_with_caller_as_owner_when_plant_does_not_exist()
    {
        await using var db = BuildContext("plant-new");
        var caller = Guid.NewGuid();

        var pictureService = new RecordingPictureService();
        var handler = new IngestDetectionCommandHandler(
            new TestContext(db),
            pictureService,
            new FakeCurrentUser(caller),
            TimeProvider.System,
            NullLogger<IngestDetectionCommandHandler>.Instance);

        var result = await handler.Handle(
            BuildCommand("plant-new", 1L),
            default);

        result.IsNewPlant.Should().BeTrue();
        result.IsDuplicate.Should().BeFalse();

        var plant = await db.Plants.SingleAsync(p => p.Id == "plant-new");
        plant.OwnerUserId.Should().Be(caller);
    }

    [Fact]
    public async Task Throws_ConflictException_when_plant_owned_by_a_different_user()
    {
        await using var db = BuildContext("plant-conflict");
        var other = Guid.NewGuid();
        var caller = Guid.NewGuid();

        db.Plants.Add(new Plant
        {
            Id = "plant-X",
            OwnerUserId = other,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var pictureService = new RecordingPictureService();
        var handler = new IngestDetectionCommandHandler(
            new TestContext(db),
            pictureService,
            new FakeCurrentUser(caller),
            TimeProvider.System,
            NullLogger<IngestDetectionCommandHandler>.Instance);

        await FluentActions.Invoking(() => handler.Handle(
                BuildCommand("plant-X", 1L),
                default))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("*owned by another agriculture user*");

        // No image upload, no detection write.
        pictureService.InsertCalls.Should().Be(0);
        (await db.Detections.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Returns_new_detection_201_path_when_plant_owned_by_caller()
    {
        await using var db = BuildContext("plant-owned");
        var caller = Guid.NewGuid();

        db.Plants.Add(new Plant
        {
            Id = "plant-owned",
            OwnerUserId = caller,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var pictureService = new RecordingPictureService();
        var handler = new IngestDetectionCommandHandler(
            new TestContext(db),
            pictureService,
            new FakeCurrentUser(caller),
            TimeProvider.System,
            NullLogger<IngestDetectionCommandHandler>.Instance);

        var result = await handler.Handle(
            BuildCommand("plant-owned", 7L),
            default);

        result.IsNewPlant.Should().BeFalse();
        result.IsDuplicate.Should().BeFalse();
        result.PictureId.Should().NotBeNull();
        pictureService.InsertCalls.Should().Be(1);

        (await db.Detections.SingleAsync()).PlantId.Should().Be("plant-owned");
        (await db.DetectionPictures.SingleAsync()).PictureId.Should().Be(pictureService.InsertedId);
    }

    private static IngestDetectionCommand BuildCommand(string plantId, long frameId) =>
        new(
            ImageBytes: new byte[] { 0x89, 0x50, 0x4e, 0x47 },
            MimeType: "image/png",
            FrameId: frameId,
            Timestamp: DateTimeOffset.UtcNow,
            Severity: Severity.Warning,
            Predictions: new[]
            {
                new ClassPredictionDto(DiseaseClass.EarlyBlight, 0.9, "Early Blight"),
            },
            BoundingBox: new BoundingBoxDto(0.1, 0.2, 0.3, 0.4, 0.5, 10.0),
            InferenceMs: 25,
            ConfidenceGatePassed: true,
            Row: 1,
            PlantId: plantId,
            PositionMeters: 5.0);

    private static Detection BuildDetection(string plantId, long frameId) => new()
    {
        Id = Guid.NewGuid(),
        FrameId = frameId,
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

    private static AppDbContextForTests BuildContext(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContextForTests>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContextForTests(options);
    }

    private sealed class FakeCurrentUser(Guid? userId) : ICurrentUserAccessor
    {
        public Guid? UserId => userId;
        public bool IsAuthenticated => userId is not null;
        public bool IsAdmin => false;
        public bool IsAgriculture => userId is not null;
        public bool IsSystem => false;
        public Guid RequireUserId() => userId ?? throw new InvalidOperationException("no user");
    }

    private sealed class RecordingPictureService : IPictureService
    {
        public int InsertCalls { get; private set; }
        public Guid InsertedId { get; private set; }

        public Task<Picture> InsertAsync(byte[] binary, string mimeType, string? seoFilename = null,
            string? altAttribute = null, string? titleAttribute = null, CancellationToken cancellationToken = default)
        {
            InsertCalls++;
            InsertedId = Guid.NewGuid();
            return Task.FromResult(new Picture
            {
                Id = InsertedId,
                MimeType = mimeType,
                SeoFilename = seoFilename,
                AltAttribute = altAttribute,
                TitleAttribute = titleAttribute,
                IsNew = true,
                VirtualPath = $"pictures/{InsertedId:N}.png",
            });
        }

        public Task<Picture> RegisterExternalAsync(string virtualPath, string mimeType,
            string? altAttribute = null, string? titleAttribute = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Picture?> GetByIdAsync(Guid pictureId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, Picture>> GetByIdsAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<string?> GetUrlAsync(Guid pictureId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public string GetUrl(Picture picture) => throw new NotSupportedException();

        public Task<byte[]?> GetBinaryAsync(Picture picture, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(Picture picture, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Picture> UpdateMetadataAsync(Guid pictureId, string? altAttribute, string? titleAttribute,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> RenameToDescriptiveKeyAsync(Guid pictureId, string seoFilename,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task MarkProcessedAsync(Guid pictureId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class TestContext(AppDbContextForTests inner) : IApplicationDbContext
    {
        public DbSet<RefreshToken> RefreshTokens => inner.RefreshTokens;
        public DbSet<Detection> Detections => inner.Detections;
        public DbSet<ClassPrediction> Predictions => inner.Predictions;
        public DbSet<Plant> Plants => inner.Plants;
        public DbSet<Picture> Pictures => inner.Pictures;
        public DbSet<ApiKey> ApiKeys => inner.ApiKeys;
        public DbSet<DetectionPicture> DetectionPictures => inner.DetectionPictures;
        public Task<int> SaveChangesAsync(CancellationToken ct) => inner.SaveChangesAsync(ct);
    }

    private sealed class AppDbContextForTests(DbContextOptions<AppDbContextForTests> options) : DbContext(options)
    {
        public DbSet<Plant> Plants => Set<Plant>();
        public DbSet<Detection> Detections => Set<Detection>();
        public DbSet<ClassPrediction> Predictions => Set<ClassPrediction>();
        public DbSet<Picture> Pictures => Set<Picture>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
        public DbSet<DetectionPicture> DetectionPictures => Set<DetectionPicture>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Detection>().OwnsOne(d => d.BoundingBox);
            builder.Entity<Detection>().HasMany(d => d.Predictions).WithOne().HasForeignKey(p => p.DetectionId);
            builder.Entity<ApiKey>().Ignore(k => k.IsActive);
            builder.Entity<DetectionPicture>().HasKey(dp => new { dp.DetectionId, dp.PictureId });
            base.OnModelCreating(builder);
        }
    }
}
