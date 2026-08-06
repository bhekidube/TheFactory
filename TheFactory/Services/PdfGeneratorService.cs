using System.Text;
using TheFactory.Contracts;

namespace TheFactory.Services;

public sealed class PdfGeneratorService : IPdfGeneratorService
{
    public Task<byte[]> GenerateLearnerReportPdfAsync(LearnerReportDto report, CancellationToken cancellationToken = default)
    {
        var content = $"Report: {report.FirstName} {report.Surname} - Grade {report.Grade} - Avg {report.Average}";
        var escaped = content.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        var stream = $"BT /F1 12 Tf 50 750 Td ({escaped}) Tj ET";
        var pdf = $@"%PDF-1.4
    1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj
    2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj
    3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj
    4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj
    5 0 obj << /Length {stream.Length} >> stream
    {stream}
    endstream endobj
    xref
    0 6
    0000000000 65535 f 
    0000000010 00000 n 
    0000000060 00000 n 
    0000000117 00000 n 
    0000000243 00000 n 
    0000000313 00000 n 
    trailer << /Root 1 0 R /Size 6 >>
    startxref
    420
    %%EOF";

        return Task.FromResult(Encoding.ASCII.GetBytes(pdf));
    }
}