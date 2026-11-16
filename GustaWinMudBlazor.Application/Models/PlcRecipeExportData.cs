using GustaMudBlazor.Models.Entities;

namespace GustaMudBlazor.App.Models;

public sealed record PlcRecipeExportData(
    string RecipeName,
    string RecipeSku,
    DateTime RecipeCreatedAt,
    IReadOnlyList<PlcExportRow> Rows);
