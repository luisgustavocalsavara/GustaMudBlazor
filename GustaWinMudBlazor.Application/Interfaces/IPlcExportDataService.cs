using GustaMudBlazor.App.Models;

namespace GustaMudBlazor.App.Interfaces;

public interface IPlcExportDataService
{
    Task<PlcRecipeExportData?> GetRecipeExportDataAsync(int recipeId);
}
