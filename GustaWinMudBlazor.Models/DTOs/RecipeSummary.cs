namespace GustaMudBlazor.Models.DTOs;

public sealed record RecipeSummary(
    int Id,
    string Name,
    string Sku,
    DateTime CreatedAt,
    int BlockCount);
