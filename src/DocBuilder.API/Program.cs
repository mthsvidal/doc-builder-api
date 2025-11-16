using DocBuilder.API.Endpoints;
using DocBuilder.API.Extensions;
using DocBuilder.API.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.AddConfiguration();
builder.AddDocumentation();
builder.Services.AddDomainServices();

var app = builder.Build();

// Add TrackId middleware (must be one of the first)
app.UseMiddleware<TrackIdMiddleware>();

// Add API logging middleware (after TrackId)
app.UseMiddleware<ApiLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DocBuilder API v1");
        options.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapTemplateEndpoints();

app.Run();