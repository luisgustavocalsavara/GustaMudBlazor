using System.Text;
using GustaMudBlazor.Models.Entities;

namespace GustaMudBlazor.Services.Reports;

public static class CsvReportBuilder
{
    public static string BuildPlcRecipeCsv(IReadOnlyList<PlcExportRow> rows)
    {
        var csv = new StringBuilder();
        csv.AppendLine("RECEITA;SKU;MIXER;ORDEM_INSTANCIA;CODIGO_BLOCO;NOME_BLOCO;TAG_PLC_INSTANCIA;TAG_BASE;DESCRICAO;TIPO_DADO;VALOR");

        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(";",
                EscapeCsv(row.RecipeName),
                EscapeCsv(row.RecipeSku),
                EscapeCsv(row.MixerName),
                row.BlockSequenceOrder,
                EscapeCsv(row.BlockCode),
                EscapeCsv(row.BlockName),
                EscapeCsv(row.PlcInstanceTag),
                EscapeCsv(row.Tag),
                EscapeCsv(row.Description),
                row.DataType,
                EscapeCsv(row.Value)));
        }

        return csv.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
