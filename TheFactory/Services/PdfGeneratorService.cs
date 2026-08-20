using PdfSharpCore.Drawing;
using PdfSharpCore;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using TheFactory.Contracts;

namespace TheFactory.Services;

public sealed class PdfGeneratorService : IPdfGeneratorService
{
    private const string TemplateRelativePath = "ClientApp/src/assets/Lobengula_4_Nursery_School_Letterhead.pdf";
    private const double ContentStartY = 300d;
    private const double PageMargin = 40d;
    private readonly IWebHostEnvironment _environment;

    private static readonly SubjectTemplate[] SubjectTemplates =
    {
        new("ENGLISH", new[] { "ENGLISH" }),
        new("NDEBELE", new[] { "NDEBELE" }),
        new("MATHEMATICS", new[] { "MATHEMATICS", "MATHS", "MATH" }),
        new("AGRICULTURE /\nSCIENCE & TECHNOLOGY", new[] { "AGRICULTURE", "SCIENCE", "TECHNOLOGY" }),
        new("SOCIAL SCIENCE", new[] { "SOCIAL SCIENCE", "SOCIAL" }),
        new("PHYSICAL EDUCATION\n& ARTS", new[] { "PHYSICAL EDUCATION", "ARTS", "P.E" })
    };

    public PdfGeneratorService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task<byte[]> GenerateLearnerReportPdfAsync(LearnerReportDto report, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var templatePath = Path.Combine(_environment.ContentRootPath, TemplateRelativePath);
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"PDF template was not found at '{templatePath}'.", templatePath);
        }

        using var templateDocument = PdfReader.Open(templatePath, PdfDocumentOpenMode.Import);
        if (templateDocument.PageCount == 0)
        {
            throw new InvalidOperationException("PDF template does not contain any pages.");
        }

        using var outputDocument = new PdfDocument();
        var page = outputDocument.AddPage(templateDocument.Pages[0]);
        page.Size = PageSize.A4;
        page.Orientation = PageOrientation.Portrait;

        using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
        var titleFont = new XFont("Helvetica", 14, XFontStyle.Bold);
        var headerFont = new XFont("Helvetica", 9, XFontStyle.Bold);
        var bodyFont = new XFont("Helvetica", 9, XFontStyle.Regular);
        var brush = XBrushes.Black;
        var borderPen = new XPen(XColor.FromArgb(75, 75, 75), 0.7);

        var tableWidth = page.Width - (PageMargin * 2);
        var tableX = PageMargin;

        var subjectColumnWidth = tableWidth * 0.32;
        var possibleMarkColumnWidth = tableWidth * 0.13;
        var pupilMarkColumnWidth = tableWidth * 0.13;
        var gradeColumnWidth = tableWidth * 0.12;
        var commentsColumnWidth = tableWidth - (subjectColumnWidth + possibleMarkColumnWidth + pupilMarkColumnWidth + gradeColumnWidth);

        graphics.DrawString(
            "Learner Performance Report",
            titleFont,
            brush,
            new XRect(tableX, ContentStartY, tableWidth, 24),
            XStringFormats.TopCenter);

        var startX = tableX;
        var valueX = tableX + 140d;
        var y = ContentStartY + 36d;
        var lineHeight = 16d;

        graphics.DrawString("Learner:", headerFont, brush, new XPoint(startX, y));
        graphics.DrawString($"{report.FirstName} {report.Surname}", bodyFont, XBrushes.Black, new XPoint(valueX, y));
        y += lineHeight;

        graphics.DrawString("Grade:", headerFont, brush, new XPoint(startX, y));
        graphics.DrawString(report.Grade, bodyFont, XBrushes.Black, new XPoint(valueX, y));
        y += lineHeight;

        graphics.DrawString("Learner ID:", headerFont, brush, new XPoint(startX, y));
        graphics.DrawString(report.LearnerId.ToString(), bodyFont, XBrushes.Black, new XPoint(valueX, y));
        y += lineHeight;

        graphics.DrawString("Average:", headerFont, brush, new XPoint(startX, y));
        graphics.DrawString($"{report.Average:0.##}%", bodyFont, XBrushes.Black, new XPoint(valueX, y));
        y += 24;

        var headerHeight = 28d;
        var regularRowHeight = 28d;
        var wrappedRowHeight = 40d;

        var rows = BuildAssessmentRows(report);

        DrawCell(graphics, borderPen, XBrushes.LightGray, tableX, y, subjectColumnWidth, headerHeight, "SUBJECT / LEARNING AREA", headerFont, XStringFormats.CenterLeft, 4d);
        DrawCell(graphics, borderPen, XBrushes.LightGray, tableX + subjectColumnWidth, y, possibleMarkColumnWidth, headerHeight, "POSSIBLE MARK", headerFont, XStringFormats.Center, 0);
        DrawCell(graphics, borderPen, XBrushes.LightGray, tableX + subjectColumnWidth + possibleMarkColumnWidth, y, pupilMarkColumnWidth, headerHeight, "PUPIL'S MARK", headerFont, XStringFormats.Center, 0);
        DrawCell(graphics, borderPen, XBrushes.LightGray, tableX + subjectColumnWidth + possibleMarkColumnWidth + pupilMarkColumnWidth, y, gradeColumnWidth, headerHeight, "GRADE", headerFont, XStringFormats.Center, 0);
        DrawCell(graphics, borderPen, XBrushes.LightGray, tableX + subjectColumnWidth + possibleMarkColumnWidth + pupilMarkColumnWidth + gradeColumnWidth, y, commentsColumnWidth, headerHeight, "TEACHER'S COMMENTS", headerFont, XStringFormats.CenterLeft, 4d);
        y += headerHeight;

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowHeight = row.RequiresWrap ? wrappedRowHeight : regularRowHeight;
            var rowFill = row.IsTotals ? XBrushes.Gainsboro : XBrushes.White;

            DrawCell(graphics, borderPen, rowFill, tableX, y, subjectColumnWidth, rowHeight, row.Subject, headerFont, XStringFormats.CenterLeft, 4d);
            DrawCell(graphics, borderPen, rowFill, tableX + subjectColumnWidth, y, possibleMarkColumnWidth, rowHeight, row.PossibleMarkText, bodyFont, XStringFormats.Center, 0);
            DrawCell(graphics, borderPen, rowFill, tableX + subjectColumnWidth + possibleMarkColumnWidth, y, pupilMarkColumnWidth, rowHeight, row.PupilMarkText, bodyFont, XStringFormats.Center, 0);
            DrawCell(graphics, borderPen, rowFill, tableX + subjectColumnWidth + possibleMarkColumnWidth + pupilMarkColumnWidth, y, gradeColumnWidth, rowHeight, row.GradeText, bodyFont, XStringFormats.Center, 0);
            DrawCell(graphics, borderPen, rowFill, tableX + subjectColumnWidth + possibleMarkColumnWidth + pupilMarkColumnWidth + gradeColumnWidth, y, commentsColumnWidth, rowHeight, row.CommentText, bodyFont, XStringFormats.CenterLeft, 4d);

            y += rowHeight;
        }

        y += 12;
        graphics.DrawString("CONDUCT:", headerFont, brush, new XPoint(tableX, y));
        graphics.DrawLine(borderPen, tableX + 60, y + 2, tableX + tableWidth, y + 2);

        using var stream = new MemoryStream();
        outputDocument.Save(stream, false);
        return Task.FromResult(stream.ToArray());
    }

    public Task<byte[]> GenerateLearnerTermReportPdfAsync(LearnerTermReportPreviewDto report, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var document = new PdfDocument();
        var page = document.AddPage();
        page.Size = PageSize.A4;
        page.Orientation = PageOrientation.Portrait;

        using var graphics = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Helvetica", 14, XFontStyle.Bold);
        var headerFont = new XFont("Helvetica", 9, XFontStyle.Bold);
        var bodyFont = new XFont("Helvetica", 9, XFontStyle.Regular);
        var brush = XBrushes.Black;
        var borderPen = new XPen(XColor.FromArgb(75, 75, 75), 0.7);

        var margin = 40d;
        var width = page.Width - (margin * 2);
        var y = 50d;

        graphics.DrawString("Learner Term Report", titleFont, brush, new XRect(margin, y, width, 24), XStringFormats.TopCenter);
        y += 34d;

        graphics.DrawString($"Learner: {report.LearnerName}", headerFont, brush, new XPoint(margin, y));
        y += 16d;
        graphics.DrawString($"Grade: {report.Grade}", headerFont, brush, new XPoint(margin, y));
        y += 16d;
        graphics.DrawString($"Term: {report.Term}", headerFont, brush, new XPoint(margin, y));
        y += 16d;
        graphics.DrawString($"Year: {report.Year}", headerFont, brush, new XPoint(margin, y));
        y += 22d;

        var titleW = width * 0.45;
        var typeW = width * 0.2;
        var markW = width * 0.15;
        var totalW = width * 0.2;
        var headerH = 26d;
        var rowH = 24d;

        DrawCell(graphics, borderPen, XBrushes.LightGray, margin, y, titleW, headerH, "TITLE", headerFont, XStringFormats.CenterLeft, 4d);
        DrawCell(graphics, borderPen, XBrushes.LightGray, margin + titleW, y, typeW, headerH, "WORK TYPE", headerFont, XStringFormats.CenterLeft, 4d);
        DrawCell(graphics, borderPen, XBrushes.LightGray, margin + titleW + typeW, y, markW, headerH, "MARK", headerFont, XStringFormats.Center, 0d);
        DrawCell(graphics, borderPen, XBrushes.LightGray, margin + titleW + typeW + markW, y, totalW, headerH, "TOTAL MARK", headerFont, XStringFormats.Center, 0d);
        y += headerH;

        foreach (var item in report.Assessments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DrawCell(graphics, borderPen, XBrushes.White, margin, y, titleW, rowH, item.Title, bodyFont, XStringFormats.CenterLeft, 4d);
            DrawCell(graphics, borderPen, XBrushes.White, margin + titleW, y, typeW, rowH, item.WorkType, bodyFont, XStringFormats.CenterLeft, 4d);
            DrawCell(graphics, borderPen, XBrushes.White, margin + titleW + typeW, y, markW, rowH, item.Mark.ToString(), bodyFont, XStringFormats.Center, 0d);
            DrawCell(graphics, borderPen, XBrushes.White, margin + titleW + typeW + markW, y, totalW, rowH, item.TotalMark.ToString(), bodyFont, XStringFormats.Center, 0d);
            y += rowH;

            if (y > page.Height - 50d)
            {
                break;
            }
        }

        if (report.Assessments.Count == 0)
        {
            DrawCell(graphics, borderPen, XBrushes.WhiteSmoke, margin, y, width, rowH, "No assessment records found for this term/year.", bodyFont, XStringFormats.CenterLeft, 4d);
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return Task.FromResult(stream.ToArray());
    }

    private static IReadOnlyList<AssessmentRow> BuildAssessmentRows(LearnerReportDto report)
    {
        var reportSubjects = report.Subjects.ToList();
        var assessmentRows = new List<AssessmentRow>(SubjectTemplates.Length + 1);

        foreach (var template in SubjectTemplates)
        {
            var subject = FindSubject(reportSubjects, template.MatchTerms);
            var possibleMark = subject?.PossibleMark ?? 0;
            var pupilMark = subject?.PupilMark ?? 0;

            assessmentRows.Add(new AssessmentRow(
                template.DisplayName,
                possibleMark > 0 ? possibleMark.ToString() : string.Empty,
                pupilMark > 0 ? pupilMark.ToString() : string.Empty,
                subject?.Grade ?? string.Empty,
                subject?.TeacherComments ?? string.Empty,
                template.DisplayName.Contains('\n'),
                false,
                possibleMark,
                pupilMark));
        }

        var totalPossibleMark = assessmentRows.Sum(r => r.PossibleMark);
        var totalPupilMark = assessmentRows.Sum(r => r.PupilMark);
        var passFailValue = ResolvePassFailResult(reportSubjects, report.Grade);

        assessmentRows.Add(new AssessmentRow(
            "TOTALS",
            totalPossibleMark.ToString(),
            totalPupilMark.ToString(),
            $"{report.Average:0.##}%",
            string.IsNullOrWhiteSpace(passFailValue) ? "PASS/FAIL" : $"PASS/FAIL: {passFailValue}",
            false,
            true,
            totalPossibleMark,
            totalPupilMark));

        return assessmentRows;
    }

    private static SubjectScoreDto? FindSubject(IReadOnlyCollection<SubjectScoreDto> subjects, IReadOnlyCollection<string> matchTerms)
    {
        foreach (var subject in subjects)
        {
            var normalizedSubject = Normalize(subject.Subject);
            foreach (var matchTerm in matchTerms)
            {
                if (normalizedSubject.Contains(Normalize(matchTerm), StringComparison.Ordinal))
                {
                    return subject;
                }
            }
        }

        return null;
    }

    private static string ResolvePassFailResult(IReadOnlyCollection<SubjectScoreDto> subjects, string learnerGrade)
    {
        var explicitResult = subjects
            .Select(s => s.Grade)
            .FirstOrDefault(g => string.Equals(g, "PASS", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(g, "FAIL", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(explicitResult))
        {
            return explicitResult.ToUpperInvariant();
        }

        if (string.Equals(learnerGrade, "PASS", StringComparison.OrdinalIgnoreCase)
            || string.Equals(learnerGrade, "FAIL", StringComparison.OrdinalIgnoreCase))
        {
            return learnerGrade.ToUpperInvariant();
        }

        return string.Empty;
    }

    private static string Normalize(string value)
    {
        return new string(value
            .ToUpperInvariant()
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            .ToArray());
    }

    private static void DrawCell(
        XGraphics graphics,
        XPen borderPen,
        XBrush fillBrush,
        double x,
        double y,
        double width,
        double height,
        string text,
        XFont font,
        XStringFormat format,
        double horizontalPadding)
    {
        var rect = new XRect(x, y, width, height);
        graphics.DrawRectangle(fillBrush, rect);
        graphics.DrawRectangle(borderPen, rect);

        var textRect = horizontalPadding > 0
            ? new XRect(x + horizontalPadding, y, width - (horizontalPadding * 2), height)
            : rect;
        graphics.DrawString(text, font, XBrushes.Black, textRect, format);
    }

    private sealed record SubjectTemplate(string DisplayName, string[] MatchTerms);

    private sealed record AssessmentRow(
        string Subject,
        string PossibleMarkText,
        string PupilMarkText,
        string GradeText,
        string CommentText,
        bool RequiresWrap,
        bool IsTotals,
        int PossibleMark,
        int PupilMark);
}