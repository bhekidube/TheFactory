using Microsoft.AspNetCore.Mvc;
using TheFactory.Contracts;
using TheFactory.Services;

namespace TheFactory.Controllers;

[ApiController]
[Route("api")]
public class ReportsController : ControllerBase
{
    private readonly ILearnerService _learnerService;
    private readonly IReportService _reportService;
    private readonly IPdfGeneratorService _pdfGeneratorService;

    public ReportsController(
        ILearnerService learnerService,
        IReportService reportService,
        IPdfGeneratorService pdfGeneratorService)
    {
        _learnerService = learnerService;
        _reportService = reportService;
        _pdfGeneratorService = pdfGeneratorService;
    }

    [HttpGet("learners")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LearnerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<LearnerDto>>> GetLearners(CancellationToken cancellationToken)
    {
        var learners = await _learnerService.GetLearnersForCurrentSchoolAsync(cancellationToken);
        return Ok(learners);
    }

    [HttpGet("reports/{learnerId:int}")]
    [ProducesResponseType(typeof(LearnerReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnerReportResponseDto>> GetReport(int learnerId, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetLearnerReportAsync(learnerId, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        var response = new LearnerReportResponseDto
        {
            SchoolName = report.SchoolName,
            LearnerName = $"{report.FirstName} {report.Surname}",
            Grade = report.Grade,
            Subjects = report.Subjects,
            Average = report.Average
        };

        return Ok(response);
    }

    [HttpGet("reports/{learnerId:int}/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetReportPdf(int learnerId, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetLearnerReportAsync(learnerId, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        var pdfBytes = await _pdfGeneratorService.GenerateLearnerReportPdfAsync(report, cancellationToken);
        var fileName = $"{report.FirstName}_{report.Surname}_Report.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }
}