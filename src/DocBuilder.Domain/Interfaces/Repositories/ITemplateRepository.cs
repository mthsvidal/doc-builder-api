using DocBuilder.Domain.Models;

namespace DocBuilder.Domain.Interfaces.Repositories;

public interface ITemplateRepository
{
    Task<Template?> GetByIdAsync(Guid id);
    Task<Template?> GetByNameAsync(string name);
    Task<IEnumerable<Template>> GetAllAsync();
    Task<Template> CreateAsync(Template template);
    Task<bool> UpdateAsync(Template template);
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetLatestVersionAsync(string templateName);
}
