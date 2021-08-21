using Localizer;
using SideLoader.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SideLoader
{
    /// <summary>Helpers to access useful dictionaries, lists and instances maintained by the game.</summary>
    public class References
    {
        // =================== LOCALIZATION ===================

        /// <summary>Cached LocalizationManager.m_generalLocalization reference.</summary>
        public static Dictionary<string, string> GENERAL_LOCALIZATION => LocalizationManager.Instance?.m_generalLocalization;

        /// <summary>Cached LocalizationManager.m_itemLocalization reference</summary>
        public static Dictionary<int, ItemLocalization> ITEM_LOCALIZATION => LocalizationManager.Instance?.m_itemLocalization;

        public static Dictionary<int, int> LOCALIZATION_DUPLICATE_NAME => ItemLocalization.DUPLICATE_NAME;
        public static Dictionary<int, int> LOCALIZATION_DUPLICATE_DESC => ItemLocalization.DUPLICATE_DESCRIPTION;

        // ============= RESOURCES PREFAB MANAGER =============

        /// <summary>Cached ResourcesPrefabManager.ITEM_PREFABS Dictionary</summary>
        public static Dictionary<string, Item> RPM_ITEM_PREFABS => ResourcesPrefabManager.ITEM_PREFABS;

        /// <summary>Cached ResourcesPrefabManager.EFFECTPRESET_PREFABS reference.</summary>
        public static Dictionary<int, EffectPreset> RPM_EFFECT_PRESETS => ResourcesPrefabManager.EFFECTPRESET_PREFABS;

        /// <summary>Cached ResourcesPrefabManager.STATUSEFFECT_PREFABS reference.</summary>
        public static Dictionary<string, StatusEffect> RPM_STATUS_EFFECTS => ResourcesPrefabManager.STATUSEFFECT_PREFABS;

        /// <summary>Cached ResourcesPrefabManager.ENCHANTMENT_PREFABS reference.</summary>
        public static Dictionary<int, Enchantment> ENCHANTMENT_PREFABS => ResourcesPrefabManager.ENCHANTMENT_PREFABS;

        // =================== RECIPE MANAGER ===================

        /// <summary>Cached RecipeManager.m_recipes Dictionary</summary>
        public static Dictionary<string, Recipe> ALL_RECIPES => RecipeManager.Instance?.m_recipes;

        /// <summary>Cached RecipeManager.m_recipeUIDsPerUstensils Dictionary</summary>
        public static Dictionary<Recipe.CraftingType, List<UID>> RECIPES_PER_UTENSIL => RecipeManager.Instance?.m_recipeUIDsPerUstensils;

        /// <summary>Cached RecipeManager.m_enchantmentRecipes reference.</summary>
        public static Dictionary<int, EnchantmentRecipe> ENCHANTMENT_RECIPES => RecipeManager.Instance?.m_enchantmentRecipes;

        // ============= OTHER =========== 

        public static GlobalAudioManager GLOBALAUDIOMANAGER
        {
            get
            {
                if (!m_GlobalAudioManager)
                {
                    var list = Resources.FindObjectsOfTypeAll<GlobalAudioManager>();
                    if (list.Any() && list[0])
                        m_GlobalAudioManager = list[0];
                    else
                        SL.LogWarning("Cannot find GlobalAudioManager Instance!");
                }
                return m_GlobalAudioManager;
            }
        }

        private static GlobalAudioManager m_GlobalAudioManager;
    }
}
