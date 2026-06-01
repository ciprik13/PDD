namespace AgriCure.Domain.Detections;

/// <summary>
/// Mapping row linking a <see cref="Detection"/> to its image <see cref="AgriCure.Domain.Pictures.Picture"/>.
/// Today enforced 1:1 via a unique constraint on <see cref="DetectionId"/> — one image per detection.
/// </summary>
public sealed class DetectionPicture
{
    public Guid DetectionId { get; set; }

    public Guid PictureId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
