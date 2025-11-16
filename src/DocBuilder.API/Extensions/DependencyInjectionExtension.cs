using DocBuilder.Domain.Services;
using DocBuilder.Domain.Interfaces.Integrations;
using DocBuilder.Infra.Integration;

namespace DocBuilder.API.Extensions;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<ITemplateService, TemplatesService>();
        services.AddSingleton<IMinioIntegration, MinioIntegration>();

        return services;
    }
}
