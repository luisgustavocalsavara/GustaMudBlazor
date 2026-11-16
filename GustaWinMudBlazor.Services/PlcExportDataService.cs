using GustaMudBlazor.App.Interfaces;
using GustaMudBlazor.App.Models;
using GustaMudBlazor.Models.Entities;
using GustaMudBlazor.Models.Interfaces;

namespace GustaMudBlazor.Services;

public class PlcExportDataService(IRecipeService recipeService) : IPlcExportDataService
{
    public async Task<PlcRecipeExportData?> GetRecipeExportDataAsync(int recipeId)
    {
        var recipe = await recipeService.GetRecipeWithMixersAsync(recipeId);
        if (recipe is null) return null;

        var rows = new List<PlcExportRow>();

        foreach (var mixer in recipe.Mixers.OrderBy(m => (int)m.MixerType))
        {
            foreach (var recipeBlock in mixer.RecipeBlocks.OrderBy(block => block.SequenceOrder))
            {
                foreach (var itemValue in recipeBlock.ItemValues.OrderBy(value => value.RecipeItem?.Tag ?? string.Empty))
                {
                    if (itemValue.RecipeItem is null) continue;

                    rows.Add(new PlcExportRow
                    {
                        RecipeId = recipe.Id,
                        RecipeName = recipe.Name,
                        RecipeSku = recipe.Sku,
                        MixerType = mixer.MixerType,
                        RecipeBlockId = recipeBlock.Id,
                        BlockSequenceOrder = recipeBlock.SequenceOrder,
                        BlockCode = recipeBlock.Block?.Code ?? string.Empty,
                        BlockName = recipeBlock.Block?.Name ?? string.Empty,
                        RecipeItemId = itemValue.RecipeItem.Id,
                        RecipeItemValueId = itemValue.Id,
                        Tag = itemValue.RecipeItem.Tag,
                        Description = itemValue.RecipeItem.Description,
                        DataType = itemValue.RecipeItem.DataType,
                        Value = itemValue.Value
                    });
                }
            }
        }

        return new PlcRecipeExportData(
            recipe.Name,
            recipe.Sku,
            recipe.CreatedAt,
            rows);
    }
}
