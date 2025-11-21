using MongoDB.Driver;
using DocBuilder.Infra.Data.Context;
using DocBuilder.Infra.Data.Entities;
using DocBuilder.Domain.Interfaces.Repositories;
using DocBuilder.Domain.Models;
using DocBuilder.Infra.Data.Configuration;

namespace DocBuilder.Infra.Data.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private readonly IMongoCollection<TemplateEntity> _templatesCollection;

    public TemplateRepository(MongoDbContext context, MongoDbSettings settings)
    {
        _templatesCollection = context.GetCollection<TemplateEntity>(settings.TemplatesCollectionName);
    }

    public async Task<Template?> GetByIdAsync(Guid id)
    {
        var filter = Builders<TemplateEntity>.Filter.Eq(t => t.Id, id);
        var entity = await _templatesCollection.Find(filter).FirstOrDefaultAsync();
        return entity != null ? MapToDomain(entity) : null;
    }

    public async Task<Template?> GetByNameAsync(string name)
    {
        var filter = Builders<TemplateEntity>.Filter.Eq(t => t.Name, name);
        var entity = await _templatesCollection.Find(filter).FirstOrDefaultAsync();
        return entity != null ? MapToDomain(entity) : null;
    }

    public async Task<IEnumerable<Template>> GetAllAsync()
    {
        var entities = await _templatesCollection.Find(_ => true).ToListAsync();
        return entities.Select(MapToDomain);
    }

    public async Task<Template> CreateAsync(Template template)
    {
        var entity = MapToEntity(template);
        await _templatesCollection.InsertOneAsync(entity);
        return template;
    }

    public async Task<bool> UpdateAsync(Template template)
    {
        var entity = MapToEntity(template);
        var filter = Builders<TemplateEntity>.Filter.Eq(t => t.Id, template.Id);
        var result = await _templatesCollection.ReplaceOneAsync(filter, entity);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var filter = Builders<TemplateEntity>.Filter.Eq(t => t.Id, id);
        var result = await _templatesCollection.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }

    public async Task<int> GetLatestVersionAsync(string templateName)
    {
        var filter = Builders<TemplateEntity>.Filter.Eq(t => t.Name, templateName);
        var entity = await _templatesCollection.Find(filter).FirstOrDefaultAsync();
        return entity?.Version ?? 0;
    }

    private Template MapToDomain(TemplateEntity entity)
    {
        var versions = entity.Versions.Select(v => MapVersionToDomain(v)).ToList();
        var statusHistory = entity.StatusHistory?.Select(MapStatusHistoryToDomain).ToList() ?? new List<StatusHistoryEntry>();
        
        return new Template(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.Version,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt,
            versions,
            statusHistory
        );
    }

    private TemplateVersion MapVersionToDomain(TemplateVersionEntity entity)
    {
        var statusHistory = entity.StatusHistory?.Select(MapStatusHistoryToDomain).ToList() ?? new List<StatusHistoryEntry>();
        
        // Reconstruct with status history
        var version = (TemplateVersion)Activator.CreateInstance(
            typeof(TemplateVersion),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            new object?[] { entity.Id, entity.Version, entity.FileName, entity.StoragePath ?? string.Empty, 
                           entity.UploadUrl ?? string.Empty, entity.UploadUrlExpiresAt ?? DateTime.UtcNow,
                           entity.FileSize, entity.IsActive, entity.CreatedAt, entity.UpdatedAt, statusHistory },
            null)!;

        return version;
    }

    private TemplateEntity MapToEntity(Template template)
    {
        return new TemplateEntity
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Version = template.Version,
            IsActive = template.IsActive,
            Versions = template.Versions.Select(MapVersionToEntity).ToList(),
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            StatusHistory = template.StatusHistory.Select(MapStatusHistoryToEntity).ToList()
        };
    }

    private TemplateVersionEntity MapVersionToEntity(TemplateVersion version)
    {
        return new TemplateVersionEntity
        {
            Id = version.Id,
            FileName = version.FileName,
            FileSize = version.FileSize,
            UploadUrl = version.UploadUrl,
            UploadUrlExpiresAt = version.UploadUrlExpiresAt,
            StoragePath = version.StoragePath,
            Version = version.Version,
            IsActive = version.IsActive,
            CreatedAt = version.CreatedAt,
            UpdatedAt = version.UpdatedAt,
            StatusHistory = version.StatusHistory.Select(MapStatusHistoryToEntity).ToList()
        };
    }

    private StatusHistoryEntry MapStatusHistoryToDomain(StatusHistoryEntryEntity entity)
    {
        return new StatusHistoryEntry(entity.IsActive, entity.Reason);
    }

    private StatusHistoryEntryEntity MapStatusHistoryToEntity(StatusHistoryEntry historyEntry)
    {
        return new StatusHistoryEntryEntity
        {
            IsActive = historyEntry.IsActive,
            Reason = historyEntry.Reason,
            ChangedAt = historyEntry.ChangedAt
        };
    }
}
