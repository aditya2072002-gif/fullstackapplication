using System.Security.Claims;
using fullstackapplication.Server.Models;
using fullstackapplication.Server.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace fullstackapplication.Server.Controllers;

[ApiController]
[Route("api/profile-photos")]
[Authorize]
public sealed class ProfilePhotosController(ProfilePhotoRepository photoRepository) : ControllerBase
{
    private const long MaxPhotoSizeBytes = 5 * 1024 * 1024;

    [HttpGet("{employeeAccountId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(int employeeAccountId, CancellationToken cancellationToken)
    {
        if (!CanAccess(employeeAccountId))
        {
            return Forbid();
        }

        var photo = await photoRepository.GetAsync(employeeAccountId, cancellationToken);
        return photo is null ? NoContent() : File(photo.Content, photo.ContentType);
    }

    [HttpPut("{employeeAccountId:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxPhotoSizeBytes + 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upload(
        int employeeAccountId,
        IFormFile photo,
        CancellationToken cancellationToken)
    {
        if (!CanAccess(employeeAccountId))
        {
            return Forbid();
        }

        if (photo.Length == 0 || photo.Length > MaxPhotoSizeBytes)
        {
            return BadRequest(new ProblemDetails { Title = "Profile photos must be non-empty and no larger than 5 MB." });
        }

        if (!string.Equals(photo.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ProblemDetails { Title = "Profile photos must be JPEG images." });
        }

        await using var input = photo.OpenReadStream();
        using var buffer = new MemoryStream((int)photo.Length);
        await input.CopyToAsync(buffer, cancellationToken);
        var content = buffer.ToArray();
        if (content.Length < 3 || content[0] != 0xFF || content[1] != 0xD8 || content[2] != 0xFF)
        {
            return BadRequest(new ProblemDetails { Title = "The uploaded file is not a valid JPEG image." });
        }

        var saved = await photoRepository.SaveAsync(employeeAccountId, content, "image/jpeg", cancellationToken);
        return saved ? NoContent() : NotFound();
    }

    private bool CanAccess(int employeeAccountId)
    {
        if (User.IsInRole(EmployeeRoles.Admin))
        {
            return true;
        }

        return int.TryParse(User.FindFirstValue("sub"), out var callerId)
            && callerId == employeeAccountId;
    }
}
