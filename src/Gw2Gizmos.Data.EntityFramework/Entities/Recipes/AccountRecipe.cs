using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Recipes;

/// <summary>
/// A recipe the account has learned (<c>/v2/account/recipes</c>): from a recipe sheet, by discovery, or automatically
/// at a discipline's level. An account's rows are replaced on every sync.
/// </summary>
[Table("AccountRecipes")]
[PrimaryKey(nameof(AccountId), nameof(RecipeId))]
public class AccountRecipe
{
    [MaxLength(64)]
    public string AccountId { get; set; } = "";

    public int RecipeId { get; set; }
}
