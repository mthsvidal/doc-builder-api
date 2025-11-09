using DocBuilder.Domain.DTOs;
using DocBuilder.Domain.Services;

namespace DocBuilder.API.Endpoints;

public static class TemplateEndpoints
{
    public static void MapTemplateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/template")
            .WithTags("Template")
            .WithOpenApi();

        // POST: Request submission URL to Upload Content
        group.MapPost("/upload-url", async (CreateTemplateDto dto, ITemplateService service) =>
        {
            var response = await service.RequestUploadUrlAsync(dto);
            return Results.Ok(response);
        })
        .WithName("RequestUploadUrl")
        .WithSummary("Request upload URL for template submission")
        .Produces<UploadUrlResponseDto>(StatusCodes.Status200OK);

        // GET: Get templates by ID
        group.MapGet("/{id:guid}", async (Guid id, ITemplateService service) =>
        {
            var template = await service.GetTemplateByIdAsync(id);
            return template != null ? Results.Ok(template) : Results.NotFound();
        })
        .WithName("GetTemplateById")
        .WithSummary("Get template by ID")
        .Produces<TemplateDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // PATCH: Change template status (activate/deactivate)
        group.MapPatch("/{id:guid}/status", async (Guid id, ChangeTemplateStatusDto dto, ITemplateService service) =>
        {
            var success = await service.ChangeTemplateStatusAsync(id, dto);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("ChangeTemplateStatus")
        .WithSummary("Activate or deactivate template")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // GET: Download template
        group.MapGet("/{id:guid}/download", async (Guid id, ITemplateService service) =>
        {
            var fileContent = await service.DownloadTemplateAsync(id);
            if (fileContent == null)
                return Results.NotFound();

            return Results.File(fileContent, "application/octet-stream", $"template_{id}.docx");
        })
        .WithName("DownloadTemplate")
        .WithSummary("Download template")
        .Produces<byte[]>(StatusCodes.Status200OK, "application/octet-stream")
        .Produces(StatusCodes.Status404NotFound);

        // DELETE: Remove template
        group.MapDelete("/{id:guid}", async (Guid id, ITemplateService service) =>
        {
            var success = await service.RemoveTemplateAsync(id);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveTemplate")
        .WithSummary("Remove template")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // GET: List all templates
        group.MapGet("/", async (ITemplateService service) =>
        {
            var templates = await service.ListAllTemplatesAsync();
            return Results.Ok(templates);
        })
        .WithName("ListAllTemplates")
        .WithSummary("List all templates")
        .Produces<IEnumerable<TemplateDto>>(StatusCodes.Status200OK);
    }
}
