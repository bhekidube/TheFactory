using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using TheFactory.Contracts;
using TheFactory.Services;

namespace TheFactory.Controllers;

[ApiController]
[Route("api/learners/{learnerId:int}/documents")]
public sealed class LearnerDocumentsController : ControllerBase
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

    private readonly ILearnerService _learnerService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LearnerDocumentsController> _logger;

    public LearnerDocumentsController(
        ILearnerService learnerService,
        IWebHostEnvironment environment,
        ILogger<LearnerDocumentsController> logger)
    {
        _learnerService = learnerService;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<LearnerDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<LearnerDocumentDto>>> GetLearnerDocuments(
        int learnerId,
        CancellationToken cancellationToken)
    {
        if (learnerId <= 0)
        {
            return BadRequest(new { error = "A valid learner id is required." });
        }

        var documents = await _learnerService.GetLearnerDocumentsAsync(learnerId, cancellationToken);
        return documents is null ? NotFound() : Ok(documents);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxDocumentSizeBytes + 65536)]
    [ProducesResponseType(typeof(LearnerDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnerDocumentDto>> UploadLearnerDocument(
        int learnerId,
        [FromForm] string? documentName,
        [FromForm] string? documentType,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (learnerId <= 0)
        {
            return BadRequest(new { error = "A valid learner id is required." });
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
        var allowedContentType = GetContentType(extension);
        if (allowedContentType is null
            || !string.Equals(file.ContentType, allowedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only PDF, PNG, JPEG, DOC, and DOCX documents are allowed." });
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var storageDirectory = Path.Combine(_environment.ContentRootPath, "App_Data", "learner-documents");
        var storedFilePath = Path.Combine(storageDirectory, storedFileName);
        var relativeFilePath = Path.Combine("App_Data", "learner-documents", storedFileName)
            .Replace(Path.DirectorySeparatorChar, '/');

        try
        {
            Directory.CreateDirectory(storageDirectory);
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
                TryDeleteUploadFile(storedFilePath);
                return BadRequest(new { error = "The uploaded file content does not match its file type." });
            }

            var document = await _learnerService.CreateLearnerDocumentAsync(
                learnerId,
                normalizedName,
                normalizedType,
                relativeFilePath,
                cancellationToken);

            if (document is null)
            {
                TryDeleteUploadFile(storedFilePath);
                return NotFound(new { error = "Learner was not found in the current school." });
            }

            return StatusCode(StatusCodes.Status201Created, document);
        }
        catch (Exception ex)
        {
            TryDeleteUploadFile(storedFilePath);
            _logger.LogError(ex, "Failed to upload document for learner {LearnerId}.", learnerId);
            throw;
        }
    }

    [HttpGet("{documentId:int}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadLearnerDocument(
        int learnerId,
        int documentId,
        CancellationToken cancellationToken)
    {
        var filePath = await _learnerService.GetLearnerDocumentFilePathAsync(learnerId, documentId, cancellationToken);
        if (filePath is null || !TryGetStoredFilePath(filePath, out var storedFilePath)
            || !System.IO.File.Exists(storedFilePath))
        {
            return NotFound();
        }

        var extension = Path.GetExtension(storedFilePath).ToLowerInvariant();
        var contentType = GetContentType(extension);
        if (contentType is null)
        {
            _logger.LogError("Learner document {DocumentId} has an unsupported stored file type.", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return PhysicalFile(storedFilePath, contentType, enableRangeProcessing: true);
    }

    [HttpDelete("{documentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLearnerDocument(
        int learnerId,
        int documentId,
        CancellationToken cancellationToken)
    {
        var filePath = await _learnerService.DeleteLearnerDocumentAsync(learnerId, documentId, cancellationToken);
        if (filePath is null)
        {
            return NotFound();
        }

        if (!TryGetStoredFilePath(filePath, out var storedFilePath))
        {
            _logger.LogError("Learner document {DocumentId} has an invalid stored file path.", documentId);
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
            _logger.LogError(ex, "Failed to delete stored file for learner document {DocumentId}.", documentId);
            throw;
        }

        return NoContent();
    }

    private bool TryGetStoredFilePath(string relativePath, out string storedFilePath)
    {
        var storageRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "learner-documents"));
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

    private void TryDeleteUploadFile(string filePath)
    {
        try
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch (Exception cleanupException) when (cleanupException is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(cleanupException, "Failed to clean up incomplete learner document upload {FilePath}.", filePath);
        }
    }

    private static string? GetContentType(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => null
    };

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
}
