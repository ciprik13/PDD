using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace AgriCure.Infrastructure.Storage;

/// <summary>
/// Ensures the configured <see cref="StorageOptions.DefaultBucket"/> exists on startup and applies
/// a public-read policy so the React dashboard can fetch images directly. Production deploys where
/// bucket lifecycle is owned by ops can leave <c>DefaultBucket</c> blank — this hosted service then
/// no-ops.
/// </summary>
internal sealed class MinioBucketProvisioner(
    IMinioClient client,
    IOptions<StorageOptions> options,
    ILogger<MinioBucketProvisioner> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bucket = options.Value.DefaultBucket;
        if (string.IsNullOrWhiteSpace(bucket))
        {
            logger.LogInformation("Storage:DefaultBucket is blank — skipping bucket provisioning.");
            return;
        }

        try
        {
            var exists = await client
                .BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), cancellationToken)
                .ConfigureAwait(false);

            if (!exists)
            {
                await client
                    .MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), cancellationToken)
                    .ConfigureAwait(false);
                logger.LogInformation("Created storage bucket {Bucket}", bucket);
            }

            var policy = BuildPublicReadPolicy(bucket);
            await client
                .SetPolicyAsync(
                    new SetPolicyArgs().WithBucket(bucket).WithPolicy(policy),
                    cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation("Applied public-read policy to bucket {Bucket}", bucket);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Non-fatal: in prod, ops may own bucket lifecycle and creds may lack admin rights.
            logger.LogWarning(
                ex,
                "Could not provision bucket {Bucket}. The app will keep running; ensure the bucket exists out-of-band.",
                bucket);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static string BuildPublicReadPolicy(string bucket) =>
        $$"""
        {
          "Version": "2012-10-17",
          "Statement": [
            {
              "Effect": "Allow",
              "Principal": { "AWS": ["*"] },
              "Action": ["s3:GetObject"],
              "Resource": ["arn:aws:s3:::{{bucket}}/*"]
            }
          ]
        }
        """;
}
