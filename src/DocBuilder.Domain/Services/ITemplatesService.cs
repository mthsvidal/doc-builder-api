using DocBuilder.Domain.DTOs;

namespace DocBuilder.Domain.Services;

public interface ITemplateService
{
    Task<UploadUrlResponseDto> RequestUploadUrlAsync(CreateTemplateDto dto);
    Task<TemplateDto?> GetTemplateByIdAsync(Guid id);
    Task<bool> ChangeTemplateStatusAsync(Guid id, ChangeTemplateStatusDto dto);
    Task<byte[]?> DownloadTemplateAsync(Guid id);
    Task<bool> RemoveTemplateAsync(Guid id);
    Task<IEnumerable<TemplateDto>> ListAllTemplatesAsync();
}
