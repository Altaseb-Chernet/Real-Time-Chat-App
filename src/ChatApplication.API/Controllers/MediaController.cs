using System.Security.Cryptography;
using System.Text;
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

        // Cloudinary settings — read from Cloudinary:* section.
        var cloudName    = _config["Cloudinary:CloudName"];
        var apiKey       = _config["Cloudinary:ApiKey"];
        var apiSecret    = _config["Cloudinary:ApiSecret"];
        var uploadPreset = _config["Cloudinary:UploadPreset"];

        bool hasSignedConfig   = !string.IsNullOrWhiteSpace(cloudName)
                                 && !string.IsNullOrWhiteSpace(apiKey)
                                 && !string.IsNullOrWhiteSpace(apiSecret);
        bool hasUnsignedConfig = !string.IsNullOrWhiteSpace(cloudName)
                                 && !string.IsNullOrWhiteSpace(uploadPreset);

        // Local dev fallback: if Cloudinary isn't configured, store under wwwroot/uploads.
        if (!hasSignedConfig && !hasUnsignedConfig)
        {
            return await SaveLocallyAsync(file);
        }

        var rawContentType = (file.ContentType ?? "application/octet-stream").Trim();
        var normalizedType = rawContentType.Split(';')[0].Trim().ToLowerInvariant();

        // Determine the derived media type for the client UI
        var derivedType =
            normalizedType.StartsWith("image/") ? "image" :
            normalizedType.StartsWith("video/") ? "video" :
            normalizedType.StartsWith("audio/") ? "audio" :
            "raw";

        CloudinaryDotNet.Account account;
        if (hasSignedConfig) 
            account = new CloudinaryDotNet.Account(cloudName, apiKey, apiSecret);
        else 
            account = new CloudinaryDotNet.Account(cloudName);

        var cloudinary = new CloudinaryDotNet.Cloudinary(account);

        await using var stream = file.OpenReadStream();
        var fileDesc = new CloudinaryDotNet.FileDescription(file.FileName, stream);

        // Use ImageUploadParams with ResourceType = "auto" for ALL file types.
        // Cloudinary's "auto" resource type detects and processes images, videos,
        // audio, PDFs, and raw files correctly — no manual routing needed.
        var uploadParams = new CloudinaryDotNet.Actions.AutoUploadParams
        {
            File = fileDesc
        };
        if (!hasSignedConfig && !string.IsNullOrWhiteSpace(uploadPreset))
        {
            uploadParams.UploadPreset = uploadPreset;
        }

        var uploadResult = await cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            return StatusCode(502, ApiResponse<MediaUploadResult>.Fail($"Cloudinary upload failed: {uploadResult.Error.Message}"));
        }

        var result = new MediaUploadResult
        {
            PublicId  = uploadResult.PublicId,
            Url       = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty,
            MediaType = derivedType,
            FileName  = file.FileName,
            Bytes     = uploadResult.Bytes,
            Width     = uploadResult.Width,
            Height    = uploadResult.Height,
            Duration  = null // Duration available only for video, but auto-type doesn't expose it directly
        };

        return Ok(ApiResponse<MediaUploadResult>.Ok(result));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Saves the file to wwwroot/uploads and returns a local URL.
    /// Used when Cloudinary is not configured.
    /// </summary>
    private async Task<IActionResult> SaveLocallyAsync(IFormFile file)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);

        var safeName  = Path.GetFileName(file.FileName);
        var ext       = Path.GetExtension(safeName);
        var storedName = $"{Guid.NewGuid():N}{ext}";
        var fullPath  = Path.Combine(uploadsDir, storedName);

        await using (var fs    = System.IO.File.Create(fullPath))
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
            PublicId  = storedName,
            Url       = $"/uploads/{storedName}",
            MediaType = mediaType,
            FileName  = safeName,
            Bytes     = file.Length
        };

        return Ok(ApiResponse<MediaUploadResult>.Ok(localResult));
    }

    // Response shape expected by ChatApplication.Client.ChatApiService.UploadMediaAsync()
    public class MediaUploadResult
    {
        public string  PublicId  { get; set; } = string.Empty;
        public string  Url       { get; set; } = string.Empty;
        public string  MediaType { get; set; } = string.Empty;
        public string  FileName  { get; set; } = string.Empty;
        public long    Bytes     { get; set; }
        public int?    Width     { get; set; }
        public int?    Height    { get; set; }
        public double? Duration  { get; set; }
    }
}
