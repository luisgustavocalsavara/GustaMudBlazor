using GustaMudBlazor.App.Models;
using GustaMudBlazor.Models.Entities;
using System.Globalization;
using System.Text;

namespace GustaMudBlazor.Services.Reports;

/// <summary>
/// Gerador nativo de arquivos PDF (padrão ISO 32000-1 PDF 1.4 com fonte WinAnsi Encoding para acentuação em português)
/// sem necessidade de pacotes externos de terceiros ou licenças pagas.
/// </summary>
public static class PdfReportBuilder
{
    private const int RowsPerPage = 28;

    public static byte[] BuildPlcRecipePdf(PlcRecipeExportData report)
    {
        var pages = report.Rows.Chunk(RowsPerPage).Select(rows => rows.ToList()).ToList();
        if (pages.Count == 0) pages.Add([]);

        var objects = CreateBaseObjects();
        var pageObjectIds = AddPages(report, pages, objects);
        objects[0] = Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>");
        objects[1] = Encoding.ASCII.GetBytes(
            $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pages.Count} >>");

        return SerializePdf(objects);
    }

    private static List<byte[]> CreateBaseObjects() =>
    [
        [],
        [],
        Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
        Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>")
    ];

    private static List<int> AddPages(
        PlcRecipeExportData report,
        IReadOnlyList<List<PlcExportRow>> pages,
        List<byte[]> objects)
    {
        var pageObjectIds = new List<int>(pages.Count);

        for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            int pageObjectId = objects.Count + 1;
            int contentObjectId = pageObjectId + 1;
            pageObjectIds.Add(pageObjectId);

            var content = BuildPageContentStream(report, pages[pageIndex], pageIndex + 1, pages.Count, report.Rows.Count);
            var pageDictionary =
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjectId} 0 R >>";

            objects.Add(Encoding.ASCII.GetBytes(pageDictionary));
            objects.Add(CreateContentStreamObject(content));
        }

        return pageObjectIds;
    }

    private static byte[] CreateContentStreamObject(byte[] content)
    {
        using var stream = new MemoryStream();
        WriteAscii(stream, $"<< /Length {content.Length} >>\nstream\n");
        stream.Write(content);
        WriteAscii(stream, "\nendstream");
        return stream.ToArray();
    }

    private static byte[] SerializePdf(IReadOnlyList<byte[]> objects)
    {
        using var pdf = new MemoryStream();
        WriteAscii(pdf, "%PDF-1.4\n%\u00e2\u00e3\u00cf\u00d3\n");

        var offsets = new List<long>(objects.Count);
        for (int index = 0; index < objects.Count; index++)
        {
            offsets.Add(pdf.Position);
            WriteAscii(pdf, $"{index + 1} 0 obj\n");
            pdf.Write(objects[index]);
            WriteAscii(pdf, "\nendobj\n");
        }

        long crossReferenceOffset = pdf.Position;
        WriteAscii(pdf, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (long offset in offsets)
        {
            WriteAscii(pdf, $"{offset:D10} 00000 n \n");
        }

        WriteAscii(pdf,
            $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{crossReferenceOffset}\n%%EOF");
        return pdf.ToArray();
    }

    private static byte[] BuildPageContentStream(
        PlcRecipeExportData report,
        IReadOnlyList<PlcExportRow> pageRows,
        int pageNumber,
        int totalPages,
        int totalItems)
    {
        var content = new StringBuilder();
        AppendPageHeader(content, report, pageNumber, totalPages, totalItems);
        AppendTableHeader(content);
        AppendRows(content, pageRows);
        AppendPageFooter(content);
        return Encoding.Latin1.GetBytes(content.ToString());
    }

    private static void AppendPageHeader(
        StringBuilder content,
        PlcRecipeExportData report,
        int pageNumber,
        int totalPages,
        int totalItems)
    {
        content.AppendLine("0.12 0.16 0.28 rg");
        content.AppendLine("30 525 782 45 re f");
        content.AppendLine("1 1 1 rg");
        DrawText(content, "F2", 14, 42, 550,
            $"TABELA DE PARAMETROS PLC - RECEITA: {report.RecipeName} (SKU: {report.RecipeSku})");
        DrawText(content, "F1", 9, 42, 534,
            $"Data de Criacao da Receita: {report.RecipeCreatedAt:dd/MM/yyyy HH:mm}   |   Exportado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}   |   Total de Tags: {totalItems}   |   Pagina {pageNumber}/{totalPages}");
    }

    private static void AppendTableHeader(StringBuilder content)
    {
        const double tableTop = 500;
        content.AppendLine("0.22 0.28 0.42 rg");
        content.AppendLine($"30 {F(tableTop)} 782 20 re f");
        content.AppendLine("1 1 1 rg");
        DrawText(content, "F2", 8.5, 36, tableTop + 6, "MIXER");
        DrawText(content, "F2", 8.5, 105, tableTop + 6, "INST. / BLOCO");
        DrawText(content, "F2", 8.5, 250, tableTop + 6, "TAG PLC (INSTANCIA)");
        DrawText(content, "F2", 8.5, 445, tableTop + 6, "TAG ORIGINAL");
        DrawText(content, "F2", 8.5, 575, tableTop + 6, "DESCRICAO");
        DrawText(content, "F2", 8.5, 715, tableTop + 6, "TIPO");
        DrawText(content, "F2", 8.5, 755, tableTop + 6, "VALOR");
    }

    private static void AppendRows(StringBuilder content, IReadOnlyList<PlcExportRow> rows)
    {
        const double rowHeight = 16;
        double y = 500 - rowHeight;

        for (int index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            if (index % 2 == 0)
            {
                content.AppendLine("0.95 0.96 0.98 rg");
                content.AppendLine($"30 {F(y - 4)} 782 {F(rowHeight)} re f");
            }

            content.AppendLine("0.82 0.84 0.88 RG 0.5 w");
            content.AppendLine($"30 {F(y - 4)} m 812 {F(y - 4)} l S");
            content.AppendLine("0.1 0.1 0.15 rg");
            DrawText(content, "F2", 8, 36, y, Truncate(row.MixerName, 12));
            DrawText(content, "F1", 8, 105, y, Truncate($"#{row.BlockSequenceOrder} - {row.BlockCode}", 26));
            DrawText(content, "F2", 8, 250, y, Truncate(row.PlcInstanceTag, 34));
            DrawText(content, "F1", 8, 445, y, Truncate(row.Tag, 23));
            DrawText(content, "F1", 8, 575, y, Truncate(row.Description, 26));
            DrawText(content, "F2", 8, 715, y, row.DataType.ToString());
            DrawText(content, "F2", 8, 755, y, Truncate(row.Value, 12));
            y -= rowHeight;
        }
    }

    private static void AppendPageFooter(StringBuilder content)
    {
        content.AppendLine("0.4 0.4 0.45 rg");
        DrawText(content, "F1", 8, 30, 18,
            "Gerado pelo Sistema WinForms Blazor MudBlazor - Pronto para integracao com CLP / PLC.");
    }

    private static void DrawText(StringBuilder sb, string font, double size, double x, double y, string text)
    {
        string escaped = EscapePdfString(text);
        sb.AppendLine($"BT /{font} {F(size)} Tf {F(x)} {F(y)} Td ({escaped}) Tj ET");
    }

    private static string EscapePdfString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }

    private static string Truncate(string? value, int maxChars)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxChars ? value : value[..(maxChars - 2)] + "..";
    }

    private static string F(double val) => val.ToString("0.##", CultureInfo.InvariantCulture);

    private static void WriteAscii(Stream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }
}
