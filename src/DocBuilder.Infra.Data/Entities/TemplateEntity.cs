using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DocBuilder.Infra.Data.Entities;

public class StatusHistoryEntryEntity
{
    [BsonElement("isActive")]
    public bool IsActive { get; set; }

    [BsonElement("reason")]
    public string Reason { get; set; } = string.Empty;

    [BsonElement("changedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ChangedAt { get; set; }
}

public class TemplateVersionEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    [BsonElement("fileSize")]
    public long FileSize { get; set; }

    [BsonElement("uploadUrl")]
    public string? UploadUrl { get; set; }

    [BsonElement("uploadUrlExpiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? UploadUrlExpiresAt { get; set; }

    [BsonElement("storagePath")]
    public string? StoragePath { get; set; }

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; }

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? UpdatedAt { get; set; }

    [BsonElement("statusHistory")]
    public List<StatusHistoryEntryEntity> StatusHistory { get; set; } = new();
}

public class TemplateEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("name")]
    [BsonRequired]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; }

    [BsonElement("versions")]
    public List<TemplateVersionEntity> Versions { get; set; } = new();

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? UpdatedAt { get; set; }

    [BsonElement("statusHistory")]
    public List<StatusHistoryEntryEntity> StatusHistory { get; set; } = new();
}
