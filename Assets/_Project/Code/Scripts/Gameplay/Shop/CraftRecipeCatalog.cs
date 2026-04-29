using System.Collections.Generic;
using Gameplay.Equipment.Config;
using UnityEngine;

namespace Gameplay.Shop
{
    /// <summary> 静态合成配方运行时目录；由 <see cref="EquipmentDataLoader"/> / <see cref="RebuildFrom"/> 填充。 </summary>
    public static class CraftRecipeCatalog
    {
        private static readonly Dictionary<int, CraftRecipeDefinitionDto> Recipes = new Dictionary<int, CraftRecipeDefinitionDto>();

        public static IReadOnlyDictionary<int, CraftRecipeDefinitionDto> All => Recipes;

        public static void Clear() => Recipes.Clear();

        /// <summary> 载入或热更；重复的 <see cref="CraftRecipeDefinitionDto.RecipeId"/> 后者覆盖前者。 </summary>
        public static void RebuildFrom(IList<CraftRecipeDefinitionDto> list)
        {
            Recipes.Clear();
            if (list == null || list.Count == 0)
                return;

            foreach (var row in list)
            {
                if (row == null)
                    continue;

                var id = row.RecipeId;
                if (Recipes.ContainsKey(id))
                    Debug.LogWarning($"[CraftRecipeCatalog] recipe id 重复覆盖: {id}");

                Recipes[id] = row;
            }
        }

        public static bool TryGet(int recipeId, out CraftRecipeDefinitionDto recipe) =>
            Recipes.TryGetValue(recipeId, out recipe);

        public static bool IsRecipeEnabled(CraftRecipeDefinitionDto r) =>
            r != null && r.Enabled;
    }
}
