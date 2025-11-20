using DocBuilder.Domain.DTOs;
using DocBuilder.Domain.Models;
using DocBuilder.Domain.Interfaces.Integrations;
using DocBuilder.Domain.Constants;
using DocBuilder.Domain.Context;

namespace DocBuilder.Domain.Services;

public class TemplatesService : ITemplateService
{
    private readonly List<Template> _templates = new();
    private readonly IMinioIntegration _minioIntegration;

    public TemplatesService(IMinioIntegration minioIntegration)
    {
        _minioIntegration = minioIntegration;
    }
    
    public async Task<UploadUrlResponseDto> RequestUploadUrlAsync(CreateTemplateDto dto)
    {
        var trackId = RequestContext.TrackId;
        Console.WriteLine($"[TrackId: {trackId}] Starting RequestUploadUrlAsync for template: {dto.TemplateName}");
        
        // Validate file extension
        var fileExtension = Path.GetExtension(dto.FileNameWithExtension);
        if (string.IsNullOrWhiteSpace(fileExtension))
            throw new ArgumentException("FileNameWithExtension must include a file extension", nameof(dto.FileNameWithExtension));
        
        if (!fileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only ZIP files are allowed", nameof(dto.FileNameWithExtension));
        
        // Ensure bucket exists
        await _minioIntegration.EnsureBucketExistsAsync(StorageConstants.TemplatesBucketName);

        // Determine version number
        var version = await DetermineNextVersionAsync(dto.TemplateName);
        Console.WriteLine($"[TrackId: {trackId}] Using version {version} for template: {dto.TemplateName}");

        // Create object path: template-name/V1/Raw/file-name.zip or V2, V3, etc.
        var objectPath = $"{dto.TemplateName}/V{version}/Raw/{dto.FileNameWithExtension}";

        // Generate presigned upload URL with content-type restriction
        var presignedUrl = await _minioIntegration.GeneratePresignedUploadUrlAsync(
            StorageConstants.TemplatesBucketName, 
            objectPath, 
            900, // 15 minutes expiry
            "application/zip" // Only allow ZIP files
        );

        if (string.IsNullOrEmpty(presignedUrl))
            throw new InvalidOperationException("Failed to generate presigned upload URL");

        Console.WriteLine($"[TrackId: {trackId}] Successfully generated presigned URL for path: {objectPath}");
        
        return new UploadUrlResponseDto(
            Guid.NewGuid(),
            presignedUrl,
            DateTime.UtcNow.AddSeconds(900)
        );
    }

    private async Task<int> DetermineNextVersionAsync(string templateName)
    {
        var trackId = RequestContext.TrackId;
        Console.WriteLine($"[TrackId: {trackId}] Determining next version for template: {templateName}");
        
        // Check if template folder exists and has content
        var existingVersions = await _minioIntegration.ListObjectVersionsAsync(StorageConstants.TemplatesBucketName, templateName);
        
        if (existingVersions == null || !existingVersions.Any())
        {
            Console.WriteLine($"[TrackId: {trackId}] No existing versions found, starting with V1");
            return 1;
        }
        
        // Find the highest version number
        var maxVersion = existingVersions
            .Select(v => {
                var parts = v.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && parts[1].StartsWith("V") && int.TryParse(parts[1].Substring(1), out var versionNum))
                    return (int?)versionNum;
                return (int?)null;
            })
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DefaultIfEmpty(0)
            .Max();
        
        var nextVersion = maxVersion + 1;
        Console.WriteLine($"[TrackId: {trackId}] Found existing version V{maxVersion}, creating V{nextVersion}");
        
        return nextVersion;
    }

    public Task<TemplateDto?> GetTemplateByIdAsync(Guid id)
    {
        throw new NotImplementedException("GetTemplateByIdAsync not implemented");
    }

    public Task<bool> ChangeTemplateStatusAsync(Guid id, ChangeTemplateStatusDto dto)
    {
        throw new NotImplementedException("ChangeTemplateStatusAsync not implemented");
    }

    public Task<byte[]?> DownloadTemplateAsync(Guid id)
    {
        throw new NotImplementedException("DownloadTemplateAsync not implemented");
    }

    public Task<bool> RemoveTemplateAsync(Guid id)
    {
        throw new NotImplementedException("RemoveTemplateAsync not implemented");
    }

    public Task<IEnumerable<TemplateDto>> ListAllTemplatesAsync()
    {
        throw new NotImplementedException("ListAllTemplatesAsync not implemented");
    }
}
