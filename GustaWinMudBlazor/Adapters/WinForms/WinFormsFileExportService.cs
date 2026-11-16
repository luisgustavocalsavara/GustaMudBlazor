using System.Diagnostics;
using GustaMudBlazor.App.Interfaces;
using GustaMudBlazor.App.Models;

namespace GustaMudBlazor.Adapters.WinForms;

public sealed class WinFormsFileExportService : IFileExportService
{
    public async Task<FileExportResult> SaveAsync(
        string title,
        string filter,
        string extension,
        string suggestedFileName,
        byte[] content,
        bool openAfterSave)
    {
        string? path;
        try
        {
            path = await ShowSaveFileDialogAsync(title, filter, extension, SanitizeFileName(suggestedFileName));
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or ObjectDisposedException or ArgumentException)
        {
            return new FileExportResult(false, $"Não foi possível abrir a janela para salvar o arquivo: {ex.Message}", null);
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return new FileExportResult(false, "Exportação cancelada pelo usuário.", null);
        }

        try
        {
            await File.WriteAllBytesAsync(path, content);
            var message = $"Arquivo salvo com sucesso em: {path}";

            if (openAfterSave)
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or ObjectDisposedException)
                {
                    message += $" Não foi possível abrir o arquivo: {ex.Message}";
                }
            }

            return new FileExportResult(true, message, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            return new FileExportResult(false, $"Erro ao salvar o arquivo: {ex.Message}", null);
        }
    }

    private static Task<string?> ShowSaveFileDialogAsync(string title, string filter, string extension, string fileName)
    {
        var result = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialogThread = new Thread(() =>
        {
            try
            {
                using var dialog = new SaveFileDialog
                {
                    Title = title,
                    Filter = filter,
                    DefaultExt = extension,
                    FileName = fileName,
                    AddExtension = true,
                    OverwritePrompt = true
                };

                result.SetResult(dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null);
            }
            catch (Exception ex)
            {
                result.SetException(ex);
            }
        })
        {
            IsBackground = true
        };

        dialogThread.SetApartmentState(ApartmentState.STA);
        dialogThread.Start();
        return result.Task;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        return string.IsNullOrWhiteSpace(fileName)
            ? "relatorio"
            : new string(fileName.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());
    }
}
