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

        public MinioIntegration()
        {
            // Try to load .env file from multiple possible locations
            var possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".env"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", ".env")
            };

            bool envLoaded = false;
            foreach (var path in possiblePaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    DotNetEnv.Env.Load(fullPath);
                    envLoaded = true;
                    break;
                }
            }

            // Even if .env is not found, try to read from environment variables
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
                              $"MINIO_SECRET_KEY={(!string.IsNullOrEmpty(secretKey) ? "SET" : "MISSING")}. " +
                              $".env file loaded: {envLoaded}";
                
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
            Console.WriteLine($"[TrackId: {trackId}] Ensuring bucket '{bucketName}' exists");
            
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

        public async Task<string?> GeneratePresignedUploadUrlAsync(string bucketName, string objectName, int expiryInSeconds = 900)
        {
            var trackId = RequestContext.TrackId;
            Console.WriteLine($"[TrackId: {trackId}] Generating presigned URL for '{bucketName}/{objectName}'");
            
            try
            {
                var presignedPutObjectArgs = new PresignedPutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithExpiry(expiryInSeconds);

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
            Console.WriteLine($"[TrackId: {trackId}] Uploading JSON to '{bucketName}/{objectPath}'");
            
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
                Console.WriteLine($"[TrackId: {trackId}] Error uploading JSON: {ex.Message}");
                // Don't throw - logging should not break the application
            }
        }

        public async Task<IEnumerable<string>> ListObjectVersionsAsync(string bucketName, string prefix)
        {
            var trackId = RequestContext.TrackId;
            Console.WriteLine($"[TrackId: {trackId}] Listing objects in '{bucketName}' with prefix '{prefix}'");
            
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
                    {
                        objectPaths.Add(item.Key);
                    }
                }
                
                Console.WriteLine($"[TrackId: {trackId}] Found {objectPaths.Count} objects");
                return objectPaths;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TrackId: {trackId}] Error listing objects: {ex.Message}");
                return Enumerable.Empty<string>();
            }
        }
    }
}
