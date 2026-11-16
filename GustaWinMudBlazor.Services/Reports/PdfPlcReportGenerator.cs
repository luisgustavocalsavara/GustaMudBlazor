using GustaMudBlazor.App.Interfaces;
using GustaMudBlazor.App.Models;

namespace GustaMudBlazor.Services.Reports;

public sealed class PdfPlcReportGenerator : IPlcReportGenerator
{
    public string Id => "plc-pdf";
    public string DisplayName => "PDF (PLC)";
    public string FileExtension => "pdf";
    public string DialogFilter => "Documento PDF (*.pdf)|*.pdf";
    public bool OpenAfterExport => true;

    public byte[] Generate(PlcRecipeExportData report) => PdfReportBuilder.BuildPlcRecipePdf(report);
}
