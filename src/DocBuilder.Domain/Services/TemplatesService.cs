using DocBuilder.Domain.DTOs;
using DocBuilder.Domain.Models;
using DocBuilder.Domain.Interfaces.Integrations;
using DocBuilder.Domain.Interfaces.Repositories;
using DocBuilder.Domain.Constants;
using DocBuilder.Domain.Context;
using DocBuilder.Domain.Exceptions;

namespace DocBuilder.Domain.Services;

public class TemplatesService : ITemplateService
{
    private readonly IMinioIntegration _minioIntegration;
    private readonly ITemplateRepository _templateRepository;
    private readonly ILogIntegration _logIntegration;

    public TemplatesService(IMinioIntegration minioIntegration, ITemplateRepository templateRepository, ILogIntegration logIntegration)
    {
        _minioIntegration = minioIntegration;
        _templateRepository = templateRepository;
        _logIntegration = logIntegration;
    }
    
    public async Task<UploadUrlResponseDto> RequestUploadUrlAsync(CreateTemplateDto dto)
    {
        var trackId = RequestContext.TrackId;
        _logIntegration.LogInformation("Starting CreateTemplate for template: {0}", dto.TemplateName);
        
        // Validate file extension
        var fileExtension = Path.GetExtension(dto.FileNameWithExtension);
        
        if (string.IsNullOrWhiteSpace(fileExtension))
            throw new ArgumentException("FileNameWithExtension must include a file extension.", nameof(dto.FileNameWithExtension));
        
        if (!fileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only ZIP files are allowed.", nameof(dto.FileNameWithExtension));
        
        // Ensure bucket exists
        await _minioIntegration.EnsureBucketExistsAsync(StorageConstants.TemplatesBucketName);

        // Check if template already exists
        var existingTemplate = await _templateRepository.GetByNameAsync(dto.TemplateName);
        
        Template template;
        TemplateVersion newVersion;
        
        if (existingTemplate != null)
        {
            // Template exists, add new version
            _logIntegration.LogInformation("Template exists, adding version {0}", existingTemplate.Version + 1);
            
            var version = existingTemplate.Version + 1;
            var objectPath = $"{dto.TemplateName}/V{version}/Raw/{dto.FileNameWithExtension}";
            
            // Generate presigned upload URL
            var presignedUrl = await _minioIntegration.GeneratePresignedUploadUrlAsync(
                StorageConstants.TemplatesBucketName, 
                objectPath, 
                900,
                "application/zip"
            );

            if (string.IsNullOrEmpty(presignedUrl))
                throw new InvalidOperationException("Failed to generate presigned upload URL.");

            // Add new version to existing template
            newVersion = existingTemplate.AddVersion(dto.FileNameWithExtension, objectPath, presignedUrl, DateTime.UtcNow.AddSeconds(900));
            
            // Update description if provided
            if (!string.IsNullOrEmpty(dto.Description))
                existingTemplate.UpdateDescription(dto.Description);

            // Update template in database
            await _templateRepository.UpdateAsync(existingTemplate);
            
            template = existingTemplate;
        }
        else
        {
            // Create new template
            _logIntegration.LogInformation("Creating new template: {0}", dto.TemplateName);
            
            template = new Template(dto.TemplateName, dto.Description);
            
            var version = 1;
            var objectPath = $"{dto.TemplateName}/V{version}/Raw/{dto.FileNameWithExtension}";
            
            // Generate presigned upload URL
            var presignedUrl = await _minioIntegration.GeneratePresignedUploadUrlAsync(
                StorageConstants.TemplatesBucketName, 
                objectPath, 
                900,
                "application/zip"
            );

            if (string.IsNullOrEmpty(presignedUrl))
                throw new InvalidOperationException("Failed to generate presigned upload URL.");

            // Add first version
            newVersion = template.AddVersion(dto.FileNameWithExtension, objectPath, presignedUrl, DateTime.UtcNow.AddSeconds(900));

            // Save to database
            await _templateRepository.CreateAsync(template);
        }

        _logIntegration.LogInformation("Template {0} version {1} created, presigned URL generated", template.Id, newVersion.Version);
        
        return new UploadUrlResponseDto(
            template.Id,
            template.Name,
            newVersion.UploadUrl!,
            newVersion.UploadUrlExpiresAt!.Value,
            newVersion.StoragePath!,
            newVersion.Version,
            template.CreatedAt,
            newVersion.Id
        );
    }

    public async Task<TemplateDto> GetTemplateByIdAsync(Guid id, Guid? versionId = null)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        
        if (template == null)
            throw new TemplateNotFoundException(id);

        // If versionId is provided, filter for that specific version
        IEnumerable<TemplateVersion> filteredVersions;
        
        if (versionId.HasValue)
        {
            var version = template.Versions.FirstOrDefault(v => v.Id == versionId.Value);
            
            if (version == null)
                throw new TemplateNotFoundException(id, versionId.Value);
            
            filteredVersions = new[] { version };
        }
        else
        {
            // Return all versions
            filteredVersions = template.Versions;
        }

        return new TemplateDto(
            template.Id,
            template.Name,
            template.Description,
            template.Version,
            template.IsActive,
            filteredVersions.Select(v => new TemplateVersionDto(
                v.Id,
                v.FileName,
                v.FileSize,
                v.UploadUrl,
                v.UploadUrlExpiresAt,
                v.StoragePath,
                v.Version,
                v.IsActive,
                v.CreatedAt,
                v.UpdatedAt
            )),
            template.CreatedAt,
            template.UpdatedAt
        );
    }

    public async Task<IEnumerable<TemplateDto>> ListAllTemplatesAsync()
    {
        var templates = await _templateRepository.GetAllAsync();
        
        return templates.Select(template => new TemplateDto(
            template.Id,
            template.Name,
            template.Description,
            template.Version,
            template.IsActive,
            template.Versions.Select(v => new TemplateVersionDto(
                v.Id,
                v.FileName,
                v.FileSize,
                v.UploadUrl,
                v.UploadUrlExpiresAt,
                v.StoragePath,
                v.Version,
                v.IsActive,
                v.CreatedAt,
                v.UpdatedAt
            )),
            template.CreatedAt,
            template.UpdatedAt
        ));
    }

    public async Task<bool> ChangeTemplateStatusAsync(Guid id, ChangeTemplateStatusDto dto, Guid? versionId = null)
    {
        var trackId = RequestContext.TrackId;
        _logIntegration.LogInformation("Changing status for template {0}, versionId: {1}, isActive: {2}", id, versionId?.ToString() ?? "null", dto.IsActive);
        
        // Get template from database
        var template = await _templateRepository.GetByIdAsync(id);
        
        if (template == null)
            throw new TemplateNotFoundException(id);

        if (versionId.HasValue)
        {
            // Change status of specific version
            var version = template.GetVersionById(versionId.Value);
            
            if (version == null)
                throw new TemplateNotFoundException(id, versionId.Value);

            var reason = dto.Reason ?? (dto.IsActive ? "Versão ativada." : "Versão desativada.");
            
            if (dto.IsActive)
            {
                template.ActivateVersion(versionId.Value, reason);
            }
            else
            {
                template.DeactivateVersion(versionId.Value, reason);
            }
            
            _logIntegration.LogInformation("Version {0} status changed. Template IsActive: {1}", versionId, template.IsActive);
        }
        else
        {
            // Change status of entire template (all versions)
            var reason = dto.Reason ?? (dto.IsActive ? "Todas as versões ativadas." : "Todas as versões desativadas.");
            
            if (dto.IsActive)
            {
                template.ActivateAllVersions(reason);
            }
            else
            {
                template.DeactivateAllVersions(reason);
            }
            
            _logIntegration.LogInformation("All versions status changed to {0}", dto.IsActive);
        }

        // Update template in database
        var updated = await _templateRepository.UpdateAsync(template);
        
        return updated;
    }

    public Task<byte[]?> DownloadTemplateAsync(Guid id)
    {
        throw new NotImplementedException("DownloadTemplateAsync not implemented.");
    }

    public async Task<bool> RemoveTemplateAsync(Guid id)
    {
        var trackId = RequestContext.TrackId;
        _logIntegration.LogInformation("Removing template with ID: {0}", id);
        
        // Get template from database
        var template = await _templateRepository.GetByIdAsync(id);
        
        if (template == null)
            throw new TemplateNotFoundException(id);

        try
        {
            // Delete all versions from MinIO using prefix (template name contains all versions)
            var prefix = $"{template.Name}/";
            await _minioIntegration.DeleteObjectsByPrefixAsync(StorageConstants.TemplatesBucketName, prefix);
            
            _logIntegration.LogInformation("Deleted all files for template '{0}' from MinIO", template.Name);
        }
        catch (Exception ex)
        {
            _logIntegration.LogWarning("Failed to delete files from MinIO: {0}", ex.Message);
            // Continue with MongoDB deletion even if MinIO fails
        }

        // Delete from database
        var deleted = await _templateRepository.DeleteAsync(id);
        
        if (deleted)
            _logIntegration.LogInformation("Successfully removed template {0} from database", id);
        
        return deleted;
    }

    public async Task<bool> RemoveVersionAsync(Guid templateId, Guid versionId)
    {
        var trackId = RequestContext.TrackId;
        _logIntegration.LogInformation("Removing version {0} from template {1}", versionId, templateId);
        
        // Get template from database
        var template = await _templateRepository.GetByIdAsync(templateId);
        
        if (template == null)
            throw new TemplateNotFoundException(templateId);

        // Get the version to remove
        var version = template.GetVersionById(versionId);
        
        if (version == null)
            throw new TemplateNotFoundException(templateId, versionId);

        // Check if this is the last version
        if (template.Versions.Count == 1)
            throw new InvalidOperationException("Cannot remove the last version of a template. Use DELETE /api/template/{id} to remove the entire template.");

        try
        {
            // Delete file from MinIO
            if (!string.IsNullOrEmpty(version.StoragePath))
            {
                await _minioIntegration.DeleteObjectAsync(StorageConstants.TemplatesBucketName, version.StoragePath);
                _logIntegration.LogInformation("Deleted file '{0}' from MinIO", version.StoragePath);
            }
        }
        catch (Exception ex)
        {
            _logIntegration.LogWarning("Failed to delete file from MinIO: {0}", ex.Message);
            // Continue with database update even if MinIO fails
        }

        // Remove version from template
        template.RemoveVersion(versionId);
        
        // Update template in database
        var updated = await _templateRepository.UpdateAsync(template);
        
        if (updated)
            _logIntegration.LogInformation("Successfully removed version {0} from template {1}", versionId, templateId);
        
        return updated;
    }
}
