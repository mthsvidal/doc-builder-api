using DocBuilder.Domain.Services;

namespace DocBuilder.API.Extensions;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<ITemplateService, TemplatesService>();

        return services;
    }
}
