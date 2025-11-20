using DocBuilder.API.Endpoints;
using DocBuilder.API.Extensions;
using DocBuilder.API.Middleware;

// Load .env file from root directory
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

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