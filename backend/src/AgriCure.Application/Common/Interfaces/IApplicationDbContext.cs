using AgriCure.Domain.Detections;
using AgriCure.Domain.Identity;
using AgriCure.Domain.Pictures;
using AgriCure.Domain.Treatments;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Detection> Detections { get; }

    DbSet<ClassPrediction> Predictions { get; }

    DbSet<Plant> Plants { get; }

    DbSet<Picture> Pictures { get; }

    DbSet<DetectionPicture> DetectionPictures { get; }

    DbSet<ApiKey> ApiKeys { get; }

    DbSet<Treatment> Treatments { get; }

    DbSet<AppliedTreatment> AppliedTreatments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
