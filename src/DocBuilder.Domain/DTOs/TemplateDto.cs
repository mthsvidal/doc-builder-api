namespace DocBuilder.Domain.DTOs;

public record TemplateVersionDto(
    Guid Id,
    string FileName,
    long FileSize,
    string? UploadUrl,
    DateTime? UploadUrlExpiresAt,
    string? StoragePath,
    int Version,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record TemplateDto(
    Guid Id,
    string Name,
    string Description,
    int Version,
    bool IsActive,
    IEnumerable<TemplateVersionDto> Versions,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateTemplateDto(
    string TemplateName,
    string Description,
    string FileNameWithExtension
);

public record UploadUrlResponseDto(
    Guid TemplateId,
    string Name,
    string UploadUrl,
    DateTime ExpiresAt,
    string StoragePath,
    int Version,
    DateTime CreatedAt,
    Guid VersionId
);

public record ChangeTemplateStatusDto(
    bool IsActive,
    string? Reason
);
