using System.Text.Json;
using ChatApplication.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.API.Controllers;

[ApiController]
[Route("api/media")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly HttpClient _http = new();

    public MediaController(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file)
    {
        if (file is null || file.Length <= 0)
            return BadRequest(ApiResponse<MediaUploadResult>.Fail("File is required."));

        // Cloudinary settings (expected via env vars / docker-compose environment).
        // Example env:
        // Cloudinary__CloudName=xxxx
        // Cloudinary__UploadPreset=xxxx
        var cloudName     = _config["Cloudinary:CloudName"];
        var uploadPreset  = _config["Cloudinary:UploadPreset"];

        // Local dev fallback: if Cloudinary isn't configured, store under wwwroot/uploads
        // so the client can still send images/files.
        if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(uploadPreset))
        {
            var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsDir);

            var safeName = Path.GetFileName(file.FileName);
            var ext = Path.GetExtension(safeName);
            var storedName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsDir, storedName);

            await using (var fs = System.IO.File.Create(fullPath))
            await using (var input = file.OpenReadStream())
            {
                await input.CopyToAsync(fs);
            }

            var type = (file.ContentType ?? "").ToLowerInvariant();
            var mediaType =
                type.StartsWith("image/") ? "image" :
                type.StartsWith("video/") ? "video" :
                type.StartsWith("audio/") ? "audio" :
                "raw";

            var localResult = new MediaUploadResult
            {
                PublicId = storedName,
                Url = $"/uploads/{storedName}",
                MediaType = mediaType,
                FileName = safeName,
                Bytes = file.Length
            };

            return Ok(ApiResponse<MediaUploadResult>.Ok(localResult));
        }

        // Using unsigned upload via upload preset.
        // Cloudinary treats audio as video resource type.
        var normalizedType = (file.ContentType ?? "").ToLowerInvariant();
        var resourceType =
            normalizedType.StartsWith("image/") ? "image" :
            normalizedType.StartsWith("audio/") || normalizedType.StartsWith("video/") ? "video" :
            "raw";
        var endpoint = $"https://api.cloudinary.com/v1_1/{cloudName}/{resourceType}/upload";

        await using var stream = file.OpenReadStream();

        using var form = new MultipartFormDataContent();
        using var sc   = new StreamContent(stream);
        sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            file.ContentType ?? "application/octet-stream");
        form.Add(sc, "file", file.FileName);
        form.Add(new StringContent(uploadPreset), "upload_preset");

        using var resp = await _http.PostAsync(endpoint, form);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            return StatusCode((int)resp.StatusCode, ApiResponse<MediaUploadResult>.Fail($"Cloudinary upload failed: {json}"));

        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        var secureUrl = root.TryGetProperty("secure_url", out var su)
            ? su.GetString()
            : root.TryGetProperty("url", out var u) ? u.GetString() : null;

        var publicId = root.TryGetProperty("public_id", out var pid) ? pid.GetString() : null;
        var uploadedResourceType = root.TryGetProperty("resource_type", out var rt) ? rt.GetString() : null;

        int? width = root.TryGetProperty("width", out var w) ? w.GetInt32() : null;
        int? height = root.TryGetProperty("height", out var h) ? h.GetInt32() : null;
        double? duration = root.TryGetProperty("duration", out var d) ? d.GetDouble() : null;

        if (string.IsNullOrWhiteSpace(secureUrl) || string.IsNullOrWhiteSpace(publicId))
            return StatusCode(502, ApiResponse<MediaUploadResult>.Fail("Cloudinary upload succeeded but response is missing secure_url/public_id."));

        var contentType = normalizedType;
        var derivedType =
            contentType.StartsWith("image/") ? "image" :
            contentType.StartsWith("video/") ? "video" :
            contentType.StartsWith("audio/") ? "audio" :
            null;

        var result = new MediaUploadResult
        {
            PublicId = publicId!,
            Url = secureUrl!,
            MediaType = derivedType ?? uploadedResourceType ?? "raw",
            FileName = file.FileName,
            Bytes = file.Length,
            Width = width,
            Height = height,
            Duration = duration
        };

        return Ok(ApiResponse<MediaUploadResult>.Ok(result));
    }

    // Response shape expected by ChatApplication.Client.ChatApiService.UploadMediaAsync()
    public class MediaUploadResult
    {
        public string PublicId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long Bytes { get; set; }

        public int? Width { get; set; }
        public int? Height { get; set; }
        public double? Duration { get; set; }
    }
}

