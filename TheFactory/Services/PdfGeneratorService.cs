using PdfSharpCore.Drawing;
using PdfSharpCore;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.Globalization;
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

        using var templateDocument = OpenTemplateDocument();
        using var outputDocument = new PdfDocument();
        var page = AddTemplatePage(outputDocument, templateDocument);

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

        using var templateDocument = OpenTemplateDocument();
        using var document = new PdfDocument();
        var assessments = report.Assessments.ToList();
        const int RowsPerPdfPage = 12;
        var pageCount = Math.Max(1, (int)Math.Ceiling(assessments.Count / (double)RowsPerPdfPage));
        var titleFont = new XFont("Helvetica", 14, XFontStyle.Bold);
        var headerFont = new XFont("Helvetica", 9, XFontStyle.Bold);
        var bodyFont = new XFont("Helvetica", 9, XFontStyle.Regular);
        var borderPen = new XPen(XColor.FromArgb(75, 75, 75), 0.7);
        var margin = PageMargin;
        var headerH = 26d;
        var rowH = 24d;

        for (var pageNumber = 0; pageNumber < pageCount; pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = AddTemplatePage(document, templateDocument);
            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            var width = page.Width - (margin * 2);
            var y = ContentStartY;
            graphics.DrawString("Learner Term Report", titleFont, XBrushes.Black,
                new XRect(margin, y, width, 24), XStringFormats.TopCenter);
            y += 34d;

            graphics.DrawString($"Learner: {report.LearnerName}", headerFont, XBrushes.Black, new XPoint(margin, y));
            y += 16d;
            graphics.DrawString($"Grade: {report.Grade}", headerFont, XBrushes.Black, new XPoint(margin, y));
            y += 16d;
            graphics.DrawString($"Term: {report.Term}", headerFont, XBrushes.Black, new XPoint(margin, y));
            y += 16d;
            graphics.DrawString($"Year: {report.Year}", headerFont, XBrushes.Black, new XPoint(margin, y));
            y += 22d;

            var titleW = width * 0.45;
            var typeW = width * 0.2;
            var markW = width * 0.15;
            var totalW = width * 0.2;
            DrawCell(graphics, borderPen, XBrushes.LightGray, margin, y, titleW, headerH, "TITLE", headerFont, XStringFormats.CenterLeft, 4d);
            DrawCell(graphics, borderPen, XBrushes.LightGray, margin + titleW, y, typeW, headerH, "WORK TYPE", headerFont, XStringFormats.CenterLeft, 4d);
            DrawCell(graphics, borderPen, XBrushes.LightGray, margin + titleW + typeW, y, markW, headerH, "MARK", headerFont, XStringFormats.Center, 0d);
            DrawCell(graphics, borderPen, XBrushes.LightGray, margin + titleW + typeW + markW, y, totalW, headerH, "TOTAL MARK", headerFont, XStringFormats.Center, 0d);
            y += headerH;

            var firstRow = pageNumber * RowsPerPdfPage;
            var endRow = Math.Min(firstRow + RowsPerPdfPage, assessments.Count);
            if (assessments.Count == 0)
            {
                DrawCell(graphics, borderPen, XBrushes.WhiteSmoke, margin, y, width, rowH,
                    "No assessment records found for this term/year.", bodyFont, XStringFormats.CenterLeft, 4d);
                continue;
            }

            for (var index = firstRow; index < endRow; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = assessments[index];
                DrawCell(graphics, borderPen, XBrushes.White, margin, y, titleW, rowH, item.Title, bodyFont, XStringFormats.CenterLeft, 4d);
                DrawCell(graphics, borderPen, XBrushes.White, margin + titleW, y, typeW, rowH, item.WorkType, bodyFont, XStringFormats.CenterLeft, 4d);
                DrawCell(graphics, borderPen, XBrushes.White, margin + titleW + typeW, y, markW, rowH, item.Mark.ToString(), bodyFont, XStringFormats.Center, 0d);
                DrawCell(graphics, borderPen, XBrushes.White, margin + titleW + typeW + markW, y, totalW, rowH, item.TotalMark.ToString(), bodyFont, XStringFormats.Center, 0d);
                y += rowH;
            }
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return Task.FromResult(stream.ToArray());
    }

    public Task<byte[]> GenerateAttendanceRegisterPdfAsync(
        string className,
        AttendanceSessionDto session,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var templateDocument = OpenTemplateDocument();
        using var document = new PdfDocument();
        var titleFont = new XFont("Helvetica", 14, XFontStyle.Bold);
        var headerFont = new XFont("Helvetica", 9, XFontStyle.Bold);
        var bodyFont = new XFont("Helvetica", 9, XFontStyle.Regular);
        var borderPen = new XPen(XColor.FromArgb(75, 75, 75), 0.7);
        var margin = PageMargin;
        var rowHeight = 22d;
        var records = session.Records.ToList();
        var presentCount = records.Count(record =>
            record.Status.Equals("Present", StringComparison.OrdinalIgnoreCase));
        var totalCount = records.Count;
        var percentage = totalCount == 0 ? 0 : presentCount * 100d / totalCount;
        const int RowsPerPdfPage = 14;

        void AddPage(int pageNumber)
        {
            var page = AddTemplatePage(document, templateDocument);
            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            var width = page.Width - (margin * 2);
            var y = ContentStartY;

            graphics.DrawString("Attendance Register", titleFont, XBrushes.Black,
                new XRect(margin, y, width, 24), XStringFormats.TopCenter);
            y += 34d;
            graphics.DrawString($"Class: {className}", headerFont, XBrushes.Black, new XPoint(margin, y));
            graphics.DrawString($"Date: {session.SessionDate:yyyy-MM-dd}", headerFont, XBrushes.Black, new XPoint(margin + width * 0.55, y));
            y += 16d;
            graphics.DrawString(
                $"Attendance: {presentCount} present of {totalCount} learners ({percentage:0.0}%)",
                headerFont,
                XBrushes.Black,
                new XPoint(margin, y));
            y += 18d;

            var numberWidth = 32d;
            var statusWidth = 82d;
            var nameWidth = width - numberWidth - statusWidth;
            DrawCell(graphics, borderPen, XBrushes.LightGray, margin, y, numberWidth, rowHeight, "#", headerFont, XStringFormats.Center, 0);
            DrawCell(graphics, borderPen, XBrushes.LightGray, margin + numberWidth, y, nameWidth, rowHeight, "LEARNER", headerFont, XStringFormats.CenterLeft, 6);
            DrawCell(graphics, borderPen, XBrushes.LightGray, margin + numberWidth + nameWidth, y, statusWidth, rowHeight, "STATUS", headerFont, XStringFormats.Center, 0);
            y += rowHeight;

            for (var index = pageNumber * RowsPerPdfPage; index < Math.Min((pageNumber + 1) * RowsPerPdfPage, totalCount); index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var record = records[index];
                var fill = index % 2 == 0 ? XBrushes.White : XBrushes.WhiteSmoke;
                DrawCell(graphics, borderPen, fill, margin, y, numberWidth, rowHeight, (index + 1).ToString(), bodyFont, XStringFormats.Center, 0);
                DrawCell(graphics, borderPen, fill, margin + numberWidth, y, nameWidth, rowHeight, record.LearnerName, bodyFont, XStringFormats.CenterLeft, 6);
                DrawCell(graphics, borderPen, fill, margin + numberWidth + nameWidth, y, statusWidth, rowHeight, record.Status, bodyFont, XStringFormats.Center, 0);
                y += rowHeight;
            }
        }

        var pageCount = Math.Max(1, (int)Math.Ceiling(totalCount / (double)RowsPerPdfPage));
        for (var pageNumber = 0; pageNumber < pageCount; pageNumber++)
        {
            AddPage(pageNumber);
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return Task.FromResult(stream.ToArray());
    }

    public Task<byte[]> GenerateFinanceTransactionPdfAsync(
        FinanceTransactionDocumentDto report,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var templateDocument = OpenTemplateDocument();
        using var document = new PdfDocument();
        var page = AddTemplatePage(document, templateDocument);
        var transaction = report.Transaction;
        var isPayment = transaction.Type.Equals("CREDIT", StringComparison.OrdinalIgnoreCase);
        var title = isPayment ? "Payment Receipt" : "Fee Invoice";
        document.Info.Title = $"{title} {transaction.TransactionId}";

        using (var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
        {
            var titleFont = new XFont("Helvetica", 14, XFontStyle.Bold);
            var labelFont = new XFont("Helvetica", 9, XFontStyle.Bold);
            var bodyFont = new XFont("Helvetica", 9, XFontStyle.Regular);
            var borderPen = new XPen(XColor.FromArgb(75, 75, 75), 0.7);
            var margin = PageMargin;
            var width = page.Width - (margin * 2);
            var rowHeight = 30d;
            var y = ContentStartY;

            graphics.DrawString(title, titleFont, XBrushes.Black,
                new XRect(margin, y, width, 24), XStringFormats.TopCenter);
            y += 44d;

            var reference = string.IsNullOrWhiteSpace(transaction.ReferenceNumber)
                ? $"TRX-{transaction.TransactionId}"
                : transaction.ReferenceNumber;
            var description = string.IsNullOrWhiteSpace(transaction.Notes)
                ? transaction.Category
                : transaction.Notes;
            var amountLabel = isPayment ? "Amount received" : "Amount charged";
            var rows = new (string Label, string Value)[]
            {
                ("Reference", reference),
                ("Learner", $"{report.LearnerName} (#{transaction.LearnerId})"),
                ("Grade", report.Grade),
                ("Date", transaction.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("Description", description),
                (amountLabel, transaction.Amount.ToString("N2", CultureInfo.GetCultureInfo("en-ZA"))),
                ("Running balance", transaction.RunningBalance.ToString("N2", CultureInfo.GetCultureInfo("en-ZA")))
            };

            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DrawCell(graphics, borderPen, XBrushes.WhiteSmoke, margin, y, width * 0.32, rowHeight,
                    row.Label, labelFont, XStringFormats.CenterLeft, 8d);
                DrawCell(graphics, borderPen, XBrushes.White, margin + width * 0.32, y, width * 0.68, rowHeight,
                    row.Value, bodyFont, XStringFormats.CenterLeft, 8d);
                y += rowHeight;
            }

            if (transaction.IsVoided)
            {
                y += 18d;
                graphics.DrawString("VOIDED TRANSACTION", labelFont, XBrushes.Black,
                    new XRect(margin, y, width, 18), XStringFormats.Center);
            }
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return Task.FromResult(stream.ToArray());
    }

    private PdfDocument OpenTemplateDocument()
    {
        var templatePath = Path.Combine(_environment.ContentRootPath, TemplateRelativePath);
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"PDF template was not found at '{templatePath}'.", templatePath);
        }

        var templateDocument = PdfReader.Open(templatePath, PdfDocumentOpenMode.Import);
        if (templateDocument.PageCount == 0)
        {
            templateDocument.Dispose();
            throw new InvalidOperationException("PDF template does not contain any pages.");
        }

        return templateDocument;
    }

    private static PdfPage AddTemplatePage(PdfDocument document, PdfDocument templateDocument)
    {
        var page = document.AddPage(templateDocument.Pages[0]);
        page.Size = PageSize.A4;
        page.Orientation = PageOrientation.Portrait;
        return page;
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