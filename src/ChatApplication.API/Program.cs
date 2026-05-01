using ChatApplication.API.Extensions;
using ChatApplication.API.Hubs;
using ChatApplication.Infrastructure.Data.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ChatApplication API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
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

var app = builder.Build();

// Tables already created via pgAdmin — skip EnsureCreatedAsync
// (it runs a slow schema introspection query when tables exist)

// Swagger always enabled so you can browse the API
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatApplication API v1");
    c.RoutePrefix = "swagger";
});

// Serve the chat UI from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseErrorHandling();
app.UseRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<PresenceHub>("/hubs/presence");

// Fallback: any unknown route → index.html (SPA-style)
app.MapFallbackToFile("index.html");

app.Run();