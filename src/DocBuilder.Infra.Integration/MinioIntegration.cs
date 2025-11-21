using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using DocBuilder.Domain.Interfaces.Integrations;
using DocBuilder.Domain.Context;

namespace DocBuilder.Infra.Integration
{
    public class MinioIntegration : IMinioIntegration
    {
        private readonly IMinioClient _minioClient;
        private readonly ILogIntegration _logIntegration;

        public MinioIntegration(ILogIntegration logIntegration)
        {
            _logIntegration = logIntegration;
            // Read from environment variables (loaded from .env in Program.cs)
            var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ?? string.Empty;
            var accessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY") ?? string.Empty;
            var secretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY") ?? string.Empty;
            var useSslStr = Environment.GetEnvironmentVariable("MINIO_USE_SSL") ?? "false";
            var useSsl = bool.TryParse(useSslStr, out var parsed) ? parsed : false;

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                var errorMsg = $"MinIO configuration is incomplete. " +
                              $"MINIO_ENDPOINT={(!string.IsNullOrEmpty(endpoint) ? "SET" : "MISSING")}, " +
                              $"MINIO_ACCESS_KEY={(!string.IsNullOrEmpty(accessKey) ? "SET" : "MISSING")}, " +
                              $"MINIO_SECRET_KEY={(!string.IsNullOrEmpty(secretKey) ? "SET" : "MISSING")}";
                
                throw new InvalidOperationException(errorMsg);
            }

            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(useSsl)
                .Build();
        }

        public async Task EnsureBucketExistsAsync(string bucketName)
        {
            var trackId = RequestContext.TrackId;
            _logIntegration.LogInformation("Ensuring bucket '{0}' exists", bucketName);
            
            try
            {
                // Check if bucket exists
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(bucketName);
                
                bool bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs);
                
                if (!bucketExists)
                {
                    // Create bucket if it doesn't exist
                    var makeBucketArgs = new MakeBucketArgs()
                        .WithBucket(bucketName);
                    
                    await _minioClient.MakeBucketAsync(makeBucketArgs);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to ensure bucket '{bucketName}' exists: {ex.Message}", ex);
            }
        }

        public async Task<string?> GeneratePresignedUploadUrlAsync(string bucketName, string objectName, int expiryInSeconds = 900, string? contentType = null)
        {
            var trackId = RequestContext.TrackId;
            _logIntegration.LogInformation("Generating presigned URL for '{0}/{1}'", bucketName, objectName);
            
            try
            {
                var presignedPutObjectArgs = new PresignedPutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithExpiry(expiryInSeconds);

                // Add content-type restriction if specified
                if (!string.IsNullOrEmpty(contentType))
                {
                    var headers = new Dictionary<string, string>
                    {
                        { "Content-Type", contentType }
                    };
                    presignedPutObjectArgs.WithHeaders(headers);
                    _logIntegration.LogInformation("Presigned URL will require Content-Type: {0}", contentType);
                }

                string presignedUrl = await _minioClient.PresignedPutObjectAsync(presignedPutObjectArgs);
                return presignedUrl;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate presigned upload URL for bucket '{bucketName}', object '{objectName}': {ex.Message}", ex);
            }
        }

        public async Task UploadJsonAsync(string bucketName, string objectPath, string jsonContent)
        {
            var trackId = RequestContext.TrackId;
            _logIntegration.LogInformation("Uploading JSON to '{0}/{1}'", bucketName, objectPath);
            
            try
            {
                // Ensure bucket exists
                await EnsureBucketExistsAsync(bucketName);

                // Convert JSON string to stream
                var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                using var stream = new MemoryStream(jsonBytes);

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectPath)
                    .WithStreamData(stream)
                    .WithObjectSize(jsonBytes.Length)
                    .WithContentType("application/json");

                await _minioClient.PutObjectAsync(putObjectArgs);
            }
            catch (Exception ex)
            {
                _logIntegration.LogError("Error uploading JSON: {0}", ex);
                // Don't throw - logging should not break the application
            }
        }

        public async Task<IEnumerable<string>> ListObjectVersionsAsync(string bucketName, string prefix)
        {
            var trackId = RequestContext.TrackId;
            _logIntegration.LogInformation("Listing objects in '{0}' with prefix '{1}'", bucketName, prefix);
            
            var objectPaths = new List<string>();
            
            try
            {
                var listObjectsArgs = new ListObjectsArgs()
                    .WithBucket(bucketName)
                    .WithPrefix(prefix)
                    .WithRecursive(true);

                var observable = _minioClient.ListObjectsEnumAsync(listObjectsArgs);
                
                await foreach (var item in observable)
                {
                    if (!string.IsNullOrEmpty(item.Key))
                        objectPaths.Add(item.Key);
                }
                
                _logIntegration.LogInformation("Found {0} objects", objectPaths.Count);
                return objectPaths;
            }
            catch (Exception ex)
            {
                _logIntegration.LogError("Error listing objects: {0}", ex);
                return Enumerable.Empty<string>();
            }
        }

        public async Task DeleteObjectAsync(string bucketName, string objectPath)
        {
            var trackId = RequestContext.TrackId;
            _logIntegration.LogInformation("Deleting object '{0}/{1}'", bucketName, objectPath);
            
            try
            {
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectPath);

                await _minioClient.RemoveObjectAsync(removeObjectArgs);
                _logIntegration.LogInformation("Successfully deleted object '{0}'", objectPath);
            }
            catch (Exception ex)
            {
                _logIntegration.LogError("Error deleting object '{0}': {1}", ex, objectPath, ex.Message);
                throw new InvalidOperationException($"Failed to delete object '{objectPath}' from bucket '{bucketName}': {ex.Message}", ex);
            }
        }

        public async Task DeleteObjectsByPrefixAsync(string bucketName, string prefix)
        {
            var trackId = RequestContext.TrackId;
            _logIntegration.LogInformation("Deleting all objects in '{0}' with prefix '{1}'", bucketName, prefix);
            
            try
            {
                // List all objects with the prefix
                var objectPaths = await ListObjectVersionsAsync(bucketName, prefix);
                
                if (!objectPaths.Any())
                {
                    _logIntegration.LogInformation("No objects found with prefix '{0}'", prefix);
                    return;
                }

                // Delete each object
                foreach (var objectPath in objectPaths)
                    await DeleteObjectAsync(bucketName, objectPath);
                
                _logIntegration.LogInformation("Successfully deleted {0} objects with prefix '{1}'", objectPaths.Count(), prefix);
            }
            catch (Exception ex)
            {
                _logIntegration.LogError("Error deleting objects by prefix '{0}': {1}", ex, prefix, ex.Message);
                throw new InvalidOperationException($"Failed to delete objects with prefix '{prefix}' from bucket '{bucketName}': {ex.Message}", ex);
            }
        }
    }
}
