using System;
using System.Collections.Generic;
using System.Text;

namespace DocBuilder.Domain.Interfaces.Integrations
{
    public interface IMinioIntegration
    {
        /// <summary>
        /// Ensures a bucket exists, creating it if necessary.
        /// </summary>
        Task EnsureBucketExistsAsync(string bucketName);

        /// <summary>
        /// Generates a presigned URL for uploading an object to Minio.
        /// Returns the URL or null in case of an error.
        /// </summary>
        Task<string?> GeneratePresignedUploadUrlAsync(string bucketName, string objectName, int expiryInSeconds = 900, string? contentType = null);

        /// <summary>
        /// Uploads JSON content to MinIO.
        /// </summary>
        Task UploadJsonAsync(string bucketName, string objectPath, string jsonContent);

        /// <summary>
        /// Lists all object paths within a specific prefix (folder) in a bucket.
        /// </summary>
        Task<IEnumerable<string>> ListObjectVersionsAsync(string bucketName, string prefix);

        /// <summary>
        /// Deletes an object from MinIO.
        /// </summary>
        Task DeleteObjectAsync(string bucketName, string objectPath);

        /// <summary>
        /// Deletes multiple objects from MinIO that match a prefix.
        /// </summary>
        Task DeleteObjectsByPrefixAsync(string bucketName, string prefix);
    }
}
