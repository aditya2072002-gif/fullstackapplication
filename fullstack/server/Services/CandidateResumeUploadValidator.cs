using Microsoft.AspNetCore.Http;

namespace fullstackapplication.Server.Services;

public sealed record CandidateResumeFile(byte[] Content, string FileName, string ContentType);

public static class CandidateResumeUploadValidator
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;
    public const long MaxRequestSizeBytes = MaxFileSizeBytes + 1024 * 1024;

    public static async Task<(CandidateResumeFile? File, string? Error)> ValidateAndReadAsync(
        IFormFile uploadedFile,
        CancellationToken cancellationToken)
    {
        if (uploadedFile.Length == 0)
        {
            return (null, "Resume file cannot be empty.");
        }

        if (uploadedFile.Length > MaxFileSizeBytes)
        {
            return (null, "Resume must not exceed 10 MB.");
        }

        var fileName = Path.GetFileName(uploadedFile.FileName);
        var extension = Path.GetExtension(fileName);
        var contentType = extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            _ => null
        };
        if (contentType is null)
        {
            return (null, "Resume must be a PDF or DOC file.");
        }

        await using var input = uploadedFile.OpenReadStream();
        using var buffer = new MemoryStream((int)uploadedFile.Length);
        await input.CopyToAsync(buffer, cancellationToken);
        var content = buffer.ToArray();

        var validSignature = extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            ? content.AsSpan().StartsWith("%PDF-"u8)
            : content.AsSpan().StartsWith(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });
        if (!validSignature)
        {
            return (null, "Resume file content does not match its PDF or DOC extension.");
        }

        return (new CandidateResumeFile(content, fileName, contentType), null);
    }
}
