using DocBuilder.Domain.DTOs;
using DocBuilder.Domain.Models;

namespace DocBuilder.Domain.Services;

public class TemplatesService : ITemplateService
{
    private readonly List<Template> _templates = new();
    
    public Task<UploadUrlResponseDto> RequestUploadUrlAsync(CreateTemplateDto dto)
    {
        throw new NotImplementedException("RequestUploadUrlAsync not implemented");
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
