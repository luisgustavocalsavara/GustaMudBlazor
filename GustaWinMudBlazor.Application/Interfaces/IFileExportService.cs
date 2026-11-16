using GustaMudBlazor.App.Models;

namespace GustaMudBlazor.App.Interfaces;

public interface IFileExportService
{
    Task<FileExportResult> SaveAsync(
        string title,
        string filter,
        string extension,
        string suggestedFileName,
        byte[] content,
        bool openAfterSave);
}
