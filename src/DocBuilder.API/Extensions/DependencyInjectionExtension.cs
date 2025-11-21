using DocBuilder.Domain.Services;
using DocBuilder.Domain.Interfaces.Integrations;
using DocBuilder.Domain.Interfaces.Repositories;
using DocBuilder.Infra.Integration;
using DocBuilder.Infra.Data.Configuration;
using DocBuilder.Infra.Data.Context;
using DocBuilder.Infra.Data.Repositories;

namespace DocBuilder.API.Extensions;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB Configuration
        var mongoSettings = new MongoDbSettings
        {
            ConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? configuration["MongoDB:ConnectionString"] ?? string.Empty,
            DatabaseName = Environment.GetEnvironmentVariable("MONGO_DATABASE") ?? configuration["MongoDB:DatabaseName"] ?? "docbuilder",
            TemplatesCollectionName = configuration["MongoDB:TemplatesCollectionName"] ?? "templates"
        };

        services.AddSingleton(mongoSettings);
        services.AddSingleton<MongoDbContext>();

        // Repositories
        services.AddScoped<ITemplateRepository, TemplateRepository>();

        // Services
        services.AddScoped<ITemplateService, TemplatesService>();
        services.AddSingleton<IMinioIntegration, MinioIntegration>();
        services.AddSingleton<ILogIntegration, LogIntegration>();

        return services;
    }
}
