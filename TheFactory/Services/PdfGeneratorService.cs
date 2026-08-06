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
        var sb = new StringBuilder();
        var offsets = new List<int>();

        static void AppendObject(StringBuilder builder, List<int> objectOffsets, int objectId, string body)
        {
            objectOffsets.Add(builder.Length);
            builder.Append(objectId).Append(" 0 obj\n");
            builder.Append(body).Append("\n");
            builder.Append("endobj\n");
        }

        sb.Append("%PDF-1.4\n");
        AppendObject(sb, offsets, 1, "<< /Type /Catalog /Pages 2 0 R >>");
        AppendObject(sb, offsets, 2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        AppendObject(sb, offsets, 3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> >>");
        AppendObject(sb, offsets, 4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        AppendObject(sb, offsets, 5, $"<< /Length {stream.Length} >>\nstream\n{stream}\nendstream");

        var xrefStart = sb.Length;
        sb.Append("xref\n");
        sb.Append("0 6\n");
        sb.Append("0000000000 65535 f \n");

        foreach (var offset in offsets)
        {
            sb.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        sb.Append("trailer << /Root 1 0 R /Size 6 >>\n");
        sb.Append("startxref\n");
        sb.Append(xrefStart).Append("\n");
        sb.Append("%%EOF");

        return Task.FromResult(Encoding.ASCII.GetBytes(sb.ToString()));
    }
}