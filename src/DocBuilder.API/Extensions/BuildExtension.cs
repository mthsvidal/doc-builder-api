namespace DocBuilder.API.Extensions;

public static class BuildExtension
{
    public static void AddConfiguration(this WebApplicationBuilder builder)
    {
        // Add services to the container
        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();
    }

    public static void AddDocumentation(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "DocBuilder API",
                Version = "v1",
                Description = "A template-based PDF generation API.",
                Contact = new()
                {
                    Name = "Matheus Vidal",
                    Email = "matheus_vidal@outlook.com"
                }
            });
        });
    }
}
