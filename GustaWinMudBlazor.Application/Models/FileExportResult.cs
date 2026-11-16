namespace GustaMudBlazor.App.Models;

public sealed record FileExportResult(bool Success, string Message, string? FilePath);
