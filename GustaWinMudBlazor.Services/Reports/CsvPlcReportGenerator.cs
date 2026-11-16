using System.Text;
using GustaMudBlazor.App.Interfaces;
using GustaMudBlazor.App.Models;

namespace GustaMudBlazor.Services.Reports;

public sealed class CsvPlcReportGenerator : IPlcReportGenerator
{
    public string Id => "plc-csv";
    public string DisplayName => "CSV (PLC)";
    public string FileExtension => "csv";
    public string DialogFilter => "Arquivo CSV para PLC (*.csv)|*.csv";
    public bool OpenAfterExport => false;

    public byte[] Generate(PlcRecipeExportData report)
    {
        var content = Encoding.UTF8.GetBytes(CsvReportBuilder.BuildPlcRecipeCsv(report.Rows));
        return Encoding.UTF8.GetPreamble().Concat(content).ToArray();
    }
}
