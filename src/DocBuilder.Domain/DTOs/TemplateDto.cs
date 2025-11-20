namespace DocBuilder.Domain.DTOs;

public record TemplateDto(
    Guid Id,
    string Name,
    string Description,
    string FileName,
    long FileSize,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateTemplateDto(
    string TemplateName,
    string FileNameWithExtension
);

public record UploadUrlResponseDto(
    Guid TemplateId,
    string UploadUrl,
    DateTime ExpiresAt
);

public record ChangeTemplateStatusDto(
    bool IsActive,
    string? Reason
);
