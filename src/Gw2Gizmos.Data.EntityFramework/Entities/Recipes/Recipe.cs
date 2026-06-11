using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Recipes;

[Table("Recipes")]
public class Recipe
{
    public int Id { get; set; }
    public string Type { get; set; } = null!;
    public int OutputItemId { get; set; }
    public int OutputItemCount { get; set; }
    public int TimeToCraftMs { get; set; }
    public int MinRating { get; set; }
    public int? OutputUpgradeId { get; set; }
    public string ChatLink { get; set; } = null!;

    public List<RecipeDiscipline> Disciplines { get; set; } = new();
    public List<RecipeFlag> Flags { get; set; } = new();
    public List<RecipeIngredient> Ingredients { get; set; } = new();
}

[Table("RecipeDisciplines")]
public class RecipeDiscipline
{
    public int Id { get; set; }
    public string Value { get; set; } = null!;

    public int RecipeId { get; set; }

    [ForeignKey(nameof(RecipeId))]
    public Recipe Recipe { get; set; } = null!;
}

[Table("RecipeFlags")]
public class RecipeFlag
{
    public int Id { get; set; }
    public string Value { get; set; } = null!;

    public int RecipeId { get; set; }

    [ForeignKey(nameof(RecipeId))]
    public Recipe Recipe { get; set; } = null!;
}

[Table("RecipeIngredients")]
[PrimaryKey("Id", "RecipeId")]
public class RecipeIngredient
{
    public int Id { get; set; }
    public int Count { get; set; }
    public string Type { get; set; } = null!;

    [Key]
    public int RecipeId { get; set; }

    [ForeignKey(nameof(RecipeId))]
    public Recipe Recipe { get; set; } = null!;
}
