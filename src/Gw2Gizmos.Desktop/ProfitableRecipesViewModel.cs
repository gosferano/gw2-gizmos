using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Gw2Gizmos.Desktop.Mvvm;
using Gw2Gizmos.RecipeFinder;
using Gw2Gizmos.RecipeFinder.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the Profitable feed. Reads the worker's precomputed <see cref="ProfitableRecipe"/> snapshot for
/// the grid, and lazily deserializes the selected row's stored craft tree into the engine's
/// <see cref="RecipeNode"/> model so the detail pane can render it — compute stays in the worker, the UI
/// only reads and displays.
/// </summary>
public sealed class ProfitableRecipesViewModel : ViewModelBase
{
    private ProfitableRecipe? _selectedRecipe;
    private IReadOnlyList<RecipeNode> _selectedTree = Array.Empty<RecipeNode>();

    public ProfitableRecipesViewModel(IServiceScopeFactory scopeFactory)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        foreach (
            ProfitableRecipe recipe in dbContext.ProfitableRecipes.AsNoTracking().OrderByDescending(r => r.Profit)
        )
        {
            Recipes.Add(recipe);
        }

        ComputedAtUtc = Recipes.Count > 0 ? Recipes[0].ComputedAtUtc : null;
    }

    /// <summary>Most profitable first.</summary>
    public ObservableCollection<ProfitableRecipe> Recipes { get; } = new();

    /// <summary>When the worker last produced this snapshot; null when it has never run.</summary>
    public DateTimeOffset? ComputedAtUtc { get; }

    public ProfitableRecipe? SelectedRecipe
    {
        get => _selectedRecipe;
        set
        {
            if (SetProperty(ref _selectedRecipe, value))
            {
                UpdateTree();
            }
        }
    }

    /// <summary>
    /// The selected recipe's craft tree as a single-element root list (what a <c>TreeView</c> binds to).
    /// Empty when nothing is selected or the stored tree can't be parsed.
    /// </summary>
    public IReadOnlyList<RecipeNode> SelectedTree
    {
        get => _selectedTree;
        private set => SetProperty(ref _selectedTree, value);
    }

    private void UpdateTree()
    {
        if (_selectedRecipe is null || string.IsNullOrEmpty(_selectedRecipe.TreeJson))
        {
            SelectedTree = Array.Empty<RecipeNode>();
            return;
        }

        RecipeNode? root = RecipeTreeSerializer.Deserialize(_selectedRecipe.TreeJson);
        SelectedTree = root is null ? Array.Empty<RecipeNode>() : new[] { root };
    }
}
