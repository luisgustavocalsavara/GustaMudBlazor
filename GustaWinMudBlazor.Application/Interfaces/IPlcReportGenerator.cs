using GustaMudBlazor.App.Models;

namespace GustaMudBlazor.App.Interfaces;

public interface IPlcReportGenerator
{
    string Id { get; }
    string DisplayName { get; }
    string FileExtension { get; }
    string DialogFilter { get; }
    bool OpenAfterExport { get; }
    byte[] Generate(PlcRecipeExportData report);
}
