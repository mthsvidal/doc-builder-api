using DocBuilder.Domain.DTOs;
using DocBuilder.Domain.Services;
using DocBuilder.Domain.Exceptions;

namespace DocBuilder.API.Endpoints;

public static class TemplateEndpoints
{
    public static void MapTemplateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/template")
            .WithTags("Template")
            .WithOpenApi();

        // POST: Create template and get presigned upload URL
        group.MapPost("/", async (CreateTemplateDto dto, ITemplateService service) =>
        {
            var response = await service.RequestUploadUrlAsync(dto);
            return Results.Ok(response);
        })
        .WithName("CreateTemplate")
        .WithSummary("Create template and get upload URL")
        .WithDescription("Creates a new template with ID, name, and description. Generates a temporary URL for uploading the template file. Returns the template information, upload URL, and expiration time.")
        .Produces<UploadUrlResponseDto>(StatusCodes.Status200OK);

        // GET: Get templates by ID
        group.MapGet("/{id:guid}", async (Guid id, Guid? versionId, ITemplateService service) =>
        {
            try
            {
                var template = await service.GetTemplateByIdAsync(id, versionId);
                return Results.Ok(template);
            }
            catch (TemplateNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithName("GetTemplateById")
        .WithSummary("Get template by ID with optional version filter")
        .WithDescription("Returns a template by ID. Optionally filter to return only a specific version by providing versionId query parameter.")
        .Produces<TemplateDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // PATCH: Change template status (activate/deactivate)
        group.MapPatch("/{id:guid}/status", async (Guid id, Guid? versionId, ChangeTemplateStatusDto dto, ITemplateService service) =>
        {
            try
            {
                var success = await service.ChangeTemplateStatusAsync(id, dto, versionId);
                return success ? Results.NoContent() : Results.NotFound();
            }
            catch (TemplateNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithName("ChangeTemplateStatus")
        .WithSummary("Activate or deactivate template or specific version")
        .WithDescription("Changes the active status of a template or a specific version. If versionId is provided, only that version is affected. Otherwise, all versions are affected. Template status is automatically updated based on version statuses.")
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
            try
            {
                var success = await service.RemoveTemplateAsync(id);
                return success ? Results.NoContent() : Results.NotFound();
            }
            catch (TemplateNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithName("RemoveTemplate")
        .WithSummary("Remove template and all its versions")
        .WithDescription("Permanently removes the template and all its versions, including associated files.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE: Remove specific version
        group.MapDelete("/{id:guid}/version/{versionId:guid}", async (Guid id, Guid versionId, ITemplateService service) =>
        {
            try
            {
                var success = await service.RemoveVersionAsync(id, versionId);
                return success ? Results.NoContent() : Results.NotFound();
            }
            catch (TemplateNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("RemoveTemplateVersion")
        .WithSummary("Remove specific version from template")
        .WithDescription("Permanently removes a specific version from the template, including its associated file.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
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
