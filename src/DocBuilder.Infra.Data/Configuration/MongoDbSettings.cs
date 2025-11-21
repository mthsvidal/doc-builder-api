namespace DocBuilder.Infra.Data.Configuration;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string TemplatesCollectionName { get; set; } = "templates";
}
