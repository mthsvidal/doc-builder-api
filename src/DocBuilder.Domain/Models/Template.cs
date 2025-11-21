namespace DocBuilder.Domain.Models;

public class StatusHistoryEntry
{
    public bool IsActive { get; private set; }
    public string Reason { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public StatusHistoryEntry(bool isActive, string reason)
    {
        IsActive = isActive;
        Reason = reason;
        ChangedAt = DateTime.UtcNow;
    }
}

public class TemplateVersion
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; }
    public long FileSize { get; private set; }
    public string? UploadUrl { get; private set; }
    public DateTime? UploadUrlExpiresAt { get; private set; }
    public string? StoragePath { get; private set; }
    public int Version { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    private List<StatusHistoryEntry> _statusHistory;
    public IReadOnlyList<StatusHistoryEntry> StatusHistory => _statusHistory.AsReadOnly();

    public TemplateVersion(int version, string fileName, string storagePath, string uploadUrl, DateTime uploadUrlExpiresAt)
    {
        Id = Guid.NewGuid();
        Version = version;
        FileName = fileName;
        FileSize = 0;
        StoragePath = storagePath;
        UploadUrl = uploadUrl;
        UploadUrlExpiresAt = uploadUrlExpiresAt;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        _statusHistory = new List<StatusHistoryEntry>
        {
            new StatusHistoryEntry(true, "Versão criada.")
        };
    }

    public void UpdateFileInfo(long fileSize)
    {
        FileSize = fileSize;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate(string reason = "Versão desativada.")
    {
        IsActive = false;
        _statusHistory.Add(new StatusHistoryEntry(false, reason));
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate(string reason = "Versão ativada.")
    {
        IsActive = true;
        _statusHistory.Add(new StatusHistoryEntry(true, reason));
        UpdatedAt = DateTime.UtcNow;
    }

    // Constructor for reconstitution from database
    private TemplateVersion(Guid id, int version, string fileName, string storagePath, string uploadUrl, 
                           DateTime uploadUrlExpiresAt, long fileSize, bool isActive, DateTime createdAt, 
                           DateTime? updatedAt, List<StatusHistoryEntry> statusHistory)
    {
        Id = id;
        Version = version;
        FileName = fileName;
        StoragePath = storagePath;
        UploadUrl = uploadUrl;
        UploadUrlExpiresAt = uploadUrlExpiresAt;
        FileSize = fileSize;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        _statusHistory = statusHistory ?? new List<StatusHistoryEntry>();
    }
}

public class Template
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Version { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    private List<TemplateVersion> _versions;
    public IReadOnlyList<TemplateVersion> Versions => _versions.AsReadOnly();
    private List<StatusHistoryEntry> _statusHistory;
    public IReadOnlyList<StatusHistoryEntry> StatusHistory => _statusHistory.AsReadOnly();

    public Template(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Version = 0;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        _versions = new List<TemplateVersion>();
        _statusHistory = new List<StatusHistoryEntry>
        {
            new StatusHistoryEntry(true, "Template criado.")
        };
    }

    public TemplateVersion AddVersion(string fileName, string storagePath, string uploadUrl, DateTime uploadUrlExpiresAt)
    {
        Version++;
        var newVersion = new TemplateVersion(Version, fileName, storagePath, uploadUrl, uploadUrlExpiresAt);
        _versions.Add(newVersion);
        UpdatedAt = DateTime.UtcNow;
        return newVersion;
    }

    public TemplateVersion? GetLatestVersion()
    {
        return _versions.OrderByDescending(v => v.Version).FirstOrDefault();
    }

    public TemplateVersion? GetVersionByNumber(int versionNumber)
    {
        return _versions.FirstOrDefault(v => v.Version == versionNumber);
    }

    public TemplateVersion? GetVersionById(Guid versionId)
    {
        return _versions.FirstOrDefault(v => v.Id == versionId);
    }

    public bool RemoveVersion(Guid versionId)
    {
        var version = _versions.FirstOrDefault(v => v.Id == versionId);
        
        if (version == null)
            return false;

        _versions.Remove(version);
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate(string reason = "Template ativado.")
    {
        IsActive = true;
        _statusHistory.Add(new StatusHistoryEntry(true, reason));
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate(string reason = "Template desativado.")
    {
        IsActive = false;
        _statusHistory.Add(new StatusHistoryEntry(false, reason));
        UpdatedAt = DateTime.UtcNow;
    }

    public void ActivateVersion(Guid versionId, string reason = "Versão ativada.")
    {
        var version = GetVersionById(versionId);
        
        if (version != null)
        {
            version.Activate(reason);
            UpdateTemplateStatusBasedOnVersions();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void DeactivateVersion(Guid versionId, string reason = "Versão desativada.")
    {
        var version = GetVersionById(versionId);
        
        if (version != null)
        {
            version.Deactivate(reason);
            UpdateTemplateStatusBasedOnVersions();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void ActivateAllVersions(string reason = "Todas as versões ativadas.")
    {
        foreach (var version in _versions)
            version.Activate(reason);

        IsActive = true;
        _statusHistory.Add(new StatusHistoryEntry(true, reason));
        UpdatedAt = DateTime.UtcNow;
    }

    public void DeactivateAllVersions(string reason = "Todas as versões desativadas.")
    {
        foreach (var version in _versions)
            version.Deactivate(reason);

        IsActive = false;
        _statusHistory.Add(new StatusHistoryEntry(false, reason));
        UpdatedAt = DateTime.UtcNow;
    }

    private void UpdateTemplateStatusBasedOnVersions()
    {
        // If any version is active, template is active
        // If all versions are inactive, template is inactive
        var shouldBeActive = _versions.Any(v => v.IsActive);
        
        if (IsActive != shouldBeActive)
        {
            IsActive = shouldBeActive;
            var reason = shouldBeActive 
                ? "Template ativado automaticamente (versão ativa detectada)." 
                : "Template desativado automaticamente (todas as versões inativas).";
            _statusHistory.Add(new StatusHistoryEntry(shouldBeActive, reason));
        }
    }

    // Constructor for reconstitution from database
    public Template(Guid id, string name, string description, int version, bool isActive, DateTime createdAt, DateTime? updatedAt, List<TemplateVersion> versions, List<StatusHistoryEntry> statusHistory)
    {
        Id = id;
        Name = name;
        Description = description;
        Version = version;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        _versions = versions ?? new List<TemplateVersion>();
        _statusHistory = statusHistory ?? new List<StatusHistoryEntry>();
    }
}
