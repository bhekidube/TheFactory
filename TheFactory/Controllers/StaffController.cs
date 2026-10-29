using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using TheFactory.Contracts;
using TheFactory.Services;

namespace TheFactory.Controllers;

[ApiController]
[Route("api/schools/{schoolId:int}/staff")]
public sealed class StaffController : ControllerBase
{
    private const long MaxDocumentSizeBytes = 10 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedDocumentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Contract"] = "Contract",
            ["Certification"] = "Certification",
            ["Identification"] = "Identification",
            ["Other"] = "Other"
        };

    private readonly IStaffService _staffService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<StaffController> _logger;

    public StaffController(
        IStaffService staffService,
        IWebHostEnvironment environment,
        ILogger<StaffController> logger)
    {
        _staffService = staffService;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<StaffDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<StaffDto>>> GetStaff(
        int schoolId,
        [FromQuery] string? search,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        if (schoolId <= 0)
        {
            return BadRequest(new { error = "A valid school id is required." });
        }

        var staff = await _staffService.GetStaffAsync(schoolId, search, status, cancellationToken);
        return Ok(staff);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StaffDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StaffDto>> CreateStaff(
        int schoolId,
        [FromBody] StaffUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(schoolId, request);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        try
        {
            var staff = await _staffService.CreateStaffAsync(schoolId, request, cancellationToken);
            return CreatedAtAction(nameof(GetStaffByNationalId), new { schoolId, nationalId = staff.NationalId }, staff);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{nationalId}")]
    [ProducesResponseType(typeof(StaffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffDto>> GetStaffByNationalId(int schoolId, string nationalId, CancellationToken cancellationToken)
    {
        var staff = await _staffService.GetStaffByNationalIdAsync(schoolId, nationalId, cancellationToken);
        return staff is null ? NotFound() : Ok(staff);
    }

    [HttpPut("{nationalId}")]
    [ProducesResponseType(typeof(StaffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffDto>> UpdateStaff(
        int schoolId,
        string nationalId,
        [FromBody] StaffUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(schoolId, request);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        try
        {
            var staff = await _staffService.UpdateStaffAsync(schoolId, nationalId, request, cancellationToken);
            return staff is null ? NotFound() : Ok(staff);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("legacy/{id:int}")]
    [ProducesResponseType(typeof(StaffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffDto>> UpdateLegacyStaff(
        int schoolId,
        int id,
        [FromBody] StaffUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(schoolId, request);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        try
        {
            var staff = await _staffService.UpdateLegacyStaffAsync(schoolId, id, request, cancellationToken);
            return staff is null ? NotFound() : Ok(staff);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{staffId:int}/documents")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxDocumentSizeBytes + 65536)]
    [ProducesResponseType(typeof(StaffDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffDocumentDto>> UploadStaffDocument(
        int schoolId,
        int staffId,
        [FromForm] string? documentName,
        [FromForm] string? documentType,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (schoolId <= 0 || staffId <= 0)
        {
            return BadRequest(new { error = "A valid school and staff id are required." });
        }

        var normalizedName = documentName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName) || normalizedName.Length > 255)
        {
            return BadRequest(new { error = "Document name is required and must be 255 characters or fewer." });
        }

        if (string.IsNullOrWhiteSpace(documentType)
            || !AllowedDocumentTypes.TryGetValue(documentType.Trim(), out var normalizedType))
        {
            return BadRequest(new { error = "Document type must be Contract, Certification, Identification, or Other." });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "A non-empty document file is required." });
        }

        if (file.Length > MaxDocumentSizeBytes)
        {
            return BadRequest(new { error = "The document must be 10 MB or smaller." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedContentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => null
        };

        if (allowedContentType is null
            || !string.Equals(file.ContentType, allowedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only PDF, PNG, JPEG, DOC, and DOCX documents are allowed." });
        }

        var storageDirectory = Path.Combine(_environment.ContentRootPath, "App_Data", "staff-documents");
        Directory.CreateDirectory(storageDirectory);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var storedFilePath = Path.Combine(storageDirectory, storedFileName);
        var relativeFilePath = Path.Combine("App_Data", "staff-documents", storedFileName)
            .Replace(Path.DirectorySeparatorChar, '/');

        try
        {
            await using (var destination = new FileStream(
                storedFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await file.CopyToAsync(destination, cancellationToken);
            }

            if (!await HasValidFileSignatureAsync(storedFilePath, extension, cancellationToken))
            {
                System.IO.File.Delete(storedFilePath);
                return BadRequest(new { error = "The uploaded file content does not match its file type." });
            }

            var document = await _staffService.CreateStaffDocumentAsync(
                schoolId,
                staffId,
                normalizedName,
                normalizedType,
                relativeFilePath,
                cancellationToken);

            if (document is null)
            {
                System.IO.File.Delete(storedFilePath);
                return NotFound(new { error = "Staff member was not found in this school." });
            }

            return StatusCode(StatusCodes.Status201Created, document);
        }
        catch (Exception ex)
        {
            if (System.IO.File.Exists(storedFilePath))
            {
                try
                {
                    System.IO.File.Delete(storedFilePath);
                }
                catch (IOException cleanupException)
                {
                    _logger.LogError(cleanupException, "Failed to clean up incomplete staff document upload {FilePath}.", storedFilePath);
                }
            }

            _logger.LogError(ex, "Failed to upload document for staff member {StaffId} in school {SchoolId}.", staffId, schoolId);
            throw;
        }
    }

    [HttpGet("{staffId:int}/documents")]
    [ProducesResponseType(typeof(IReadOnlyCollection<StaffDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<StaffDocumentDto>>> GetStaffDocuments(
        int schoolId,
        int staffId,
        CancellationToken cancellationToken)
    {
        if (schoolId <= 0 || staffId <= 0)
        {
            return BadRequest(new { error = "A valid school and staff id are required." });
        }

        var documents = await _staffService.GetStaffDocumentsAsync(schoolId, staffId, cancellationToken);
        return documents is null ? NotFound() : Ok(documents);
    }

    [HttpGet("{staffId:int}/documents/{documentId:int}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadStaffDocument(
        int schoolId,
        int staffId,
        int documentId,
        CancellationToken cancellationToken)
    {
        if (schoolId <= 0 || staffId <= 0 || documentId <= 0)
        {
            return BadRequest(new { error = "Valid school, staff, and document ids are required." });
        }

        var relativeFilePath = await _staffService.GetStaffDocumentFilePathAsync(
            schoolId,
            staffId,
            documentId,
            cancellationToken);
        if (relativeFilePath is null || !TryGetStoredFilePath(relativeFilePath, out var storedFilePath)
            || !System.IO.File.Exists(storedFilePath))
        {
            return NotFound();
        }

        var contentType = Path.GetExtension(storedFilePath).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => null
        };
        if (contentType is null)
        {
            _logger.LogError("Staff document {DocumentId} has an unsupported stored file type.", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return PhysicalFile(storedFilePath, contentType, enableRangeProcessing: true);
    }

    [HttpDelete("{staffId:int}/documents/{documentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStaffDocument(
        int schoolId,
        int staffId,
        int documentId,
        CancellationToken cancellationToken)
    {
        if (schoolId <= 0 || staffId <= 0 || documentId <= 0)
        {
            return BadRequest(new { error = "Valid school, staff, and document ids are required." });
        }

        var relativeFilePath = await _staffService.DeleteStaffDocumentAsync(
            schoolId,
            staffId,
            documentId,
            cancellationToken);
        if (relativeFilePath is null)
        {
            return NotFound();
        }

        if (!TryGetStoredFilePath(relativeFilePath, out var storedFilePath))
        {
            _logger.LogError("Staff document {DocumentId} has an invalid stored file path.", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        try
        {
            if (System.IO.File.Exists(storedFilePath))
            {
                System.IO.File.Delete(storedFilePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to delete stored file for staff document {DocumentId}.", documentId);
            throw;
        }

        return NoContent();
    }

    private bool TryGetStoredFilePath(string relativePath, out string storedFilePath)
    {
        var storageRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "staff-documents"));
        var candidate = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, relativePath));
        var storagePrefix = storageRoot.EndsWith(Path.DirectorySeparatorChar)
            ? storageRoot
            : storageRoot + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!candidate.StartsWith(storagePrefix, comparison))
        {
            storedFilePath = string.Empty;
            return false;
        }

        storedFilePath = candidate;
        return true;
    }

    private static async Task<bool> HasValidFileSignatureAsync(
        string filePath,
        string extension,
        CancellationToken cancellationToken)
    {
        var signature = extension switch
        {
            ".pdf" => new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D },
            ".png" => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
            ".jpg" or ".jpeg" => new byte[] { 0xFF, 0xD8, 0xFF },
            ".doc" => new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 },
            ".docx" => new byte[] { 0x50, 0x4B, 0x03, 0x04 },
            _ => Array.Empty<byte>()
        };

        var header = new byte[signature.Length];
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            signature.Length,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var bytesRead = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        return bytesRead == signature.Length && CryptographicOperations.FixedTimeEquals(header, signature);
    }

    [HttpPatch("{nationalId}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveStaff(int schoolId, string nationalId, CancellationToken cancellationToken)
    {
        return await _staffService.ArchiveStaffAsync(schoolId, nationalId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpPatch("legacy/{id:int}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveLegacyStaff(int schoolId, int id, CancellationToken cancellationToken)
    {
        return await _staffService.ArchiveLegacyStaffAsync(schoolId, id, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private static string? ValidateRequest(int schoolId, StaffUpsertRequest? request)
    {
        if (schoolId <= 0)
        {
            return "A valid school id is required.";
        }

        if (request is null)
        {
            return "Staff payload is required.";
        }

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return "First name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Surname))
        {
            return "Surname is required.";
        }

        if (string.IsNullOrWhiteSpace(request.NationalId))
        {
            return "National ID is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            return "Role is required.";
        }

        if (!new[] { "Teacher", "Admin", "Support" }.Contains(request.Role.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return "Role must be Teacher, Admin, or Support.";
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return "A valid email address is required.";
        }

        if (!string.IsNullOrWhiteSpace(request.Status)
            && !request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)
            && !request.Status.Equals("Archived", StringComparison.OrdinalIgnoreCase))
        {
            return "Status must be Active or Archived.";
        }

        return null;
    }
}