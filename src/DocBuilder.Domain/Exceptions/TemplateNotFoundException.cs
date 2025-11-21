namespace DocBuilder.Domain.Exceptions;

public class TemplateNotFoundException : Exception
{
    public TemplateNotFoundException(Guid templateId) 
        : base($"Template with ID '{templateId}' was not found.")
    {
    }

    public TemplateNotFoundException(Guid templateId, Guid versionId) 
        : base($"Template with ID '{templateId}' or version with ID '{versionId}' was not found.")
    {
    }
}
