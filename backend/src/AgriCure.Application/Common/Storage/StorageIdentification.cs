namespace AgriCure.Application.Common.Storage;

/// <summary>
/// Identifies a location in object storage. The same shape is used either to address a single
/// object (Key is a full object key) or a set of objects (Key is treated as a prefix by listing
/// and bulk-copy operations).
/// </summary>
/// <param name="Bucket">Bucket name.</param>
/// <param name="Key">Object key, or key prefix for listing / bulk operations.</param>
public sealed record StorageIdentification(string Bucket, string Key)
{
    public static StorageIdentification Create(string bucket, string key) => new(bucket, key);

    public StorageIdentification WithKey(string newKey) => this with { Key = newKey };
}
