using ChatApplication.API.Extensions;
using ChatApplication.API.Hubs;
using ChatApplication.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ChatApplication API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name        = "Authorization",
        Type        = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT",
        In          = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token here"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddApplicationServices(builder.Configuration);

// Serve Blazor WebAssembly
builder.Services.AddRazorPages();

var app = builder.Build();

// Keep schema in sync on startup (important for DM/media columns).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatApplication API v1");
    c.RoutePrefix = "swagger";
});

app.UseBlazorFrameworkFiles();
// Ensure recorded audio/video files are served with correct MIME types
// so the browser can compute duration and play them (fixes "0:00" issues).
var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".ogg"]  = "audio/ogg";
contentTypes.Mappings[".opus"] = "audio/ogg";
contentTypes.Mappings[".m4a"]  = "audio/mp4";
contentTypes.Mappings[".mp3"]  = "audio/mpeg";
contentTypes.Mappings[".wav"]  = "audio/wav";
contentTypes.Mappings[".mp4"]  = "video/mp4";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypes
});

app.UseErrorHandling();
app.UseRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<PresenceHub>("/hubs/presence");

// Fallback to Blazor index.html
app.MapFallbackToFile("index.html");

app.Run();
