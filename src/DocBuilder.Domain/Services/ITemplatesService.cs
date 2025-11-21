using DocBuilder.Domain.DTOs;

namespace DocBuilder.Domain.Services;

public interface ITemplateService
{
    Task<UploadUrlResponseDto> RequestUploadUrlAsync(CreateTemplateDto dto);
    Task<TemplateDto> GetTemplateByIdAsync(Guid id, Guid? versionId = null);
    Task<bool> ChangeTemplateStatusAsync(Guid id, ChangeTemplateStatusDto dto, Guid? versionId = null);
    Task<byte[]?> DownloadTemplateAsync(Guid id);
    Task<bool> RemoveTemplateAsync(Guid id);
    Task<bool> RemoveVersionAsync(Guid templateId, Guid versionId);
    Task<IEnumerable<TemplateDto>> ListAllTemplatesAsync();
}
