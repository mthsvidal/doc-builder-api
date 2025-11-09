namespace DocBuilder.Domain.Models;

public class Template
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string FileName { get; private set; }
    public long FileSize { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Template(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        FileName = string.Empty;
        FileSize = 0;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateFileInfo(string fileName, long fileSize)
    {
        FileName = fileName;
        FileSize = fileSize;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
