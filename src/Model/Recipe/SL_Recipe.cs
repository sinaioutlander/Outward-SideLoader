using SideLoader.Model;
using SideLoader.SLPacks;
using SideLoader.SLPacks.Categories;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace SideLoader
{
    [SL_Serialized]
    public class SL_Recipe : ContentTemplate
    {
        #region IContentTemplate

        public override ITemplateCategory PackCategory => SLPackManager.GetCategoryInstance<RecipeCategory>();
        public override string DefaultTemplateName => "Untitled Recipe";

        public override bool TemplateAllowedInSubfolder => false;
        public override bool CanParseContent => true;

        public override ContentTemplate ParseToTemplate(object content) => ParseRecipe(content as Recipe);

        public override object GetContentFromID(object id)
        {
            if (string.IsNullOrEmpty((string)id))
                return null;
            References.ALL_RECIPES.TryGetValue((string)id, out Recipe ret);
            return ret;
        }

        public override void ApplyActualTemplate() => this.Internal_ApplyRecipe();

        #endregion

        [XmlIgnore]
        private bool m_applied = false;

        public string UID = "";

        public Recipe.CraftingType StationType = Recipe.CraftingType.Survival;

        public List<Ingredient> Ingredients = new List<Ingredient>();
        public List<ItemQty> Results = new List<ItemQty>();

        [Obsolete("Use 'ApplyTemplate' instead (name change).")]
        public void ApplyRecipe()
            => this.ApplyTemplate();

        internal void Internal_ApplyRecipe()
        {
            try
            {
                SL.Log("Defining recipe UID: " + this.UID);

                if (string.IsNullOrEmpty(this.UID))
                {
                    SL.LogWarning("No UID was set! Please set a UID, for example 'myname.myrecipe'. Aborting.");
                    return;
                }

                if (m_applied)
                {
                    SL.Log("Trying to apply an SL_Recipe that is already applied! This is not allowed.");
                    return;
                }

                var results = new List<ItemReferenceQuantity>();
                foreach (var result in this.Results)
                {
                    var resultItem = ResourcesPrefabManager.Instance.GetItemPrefab(result.ItemID);
                    if (!resultItem)
                    {
                        SL.Log("Error: Could not get recipe result id : " + result.ItemID);
                        return;
                    }
                    results.Add(new ItemReferenceQuantity(resultItem, result.Quantity));
                }

                var ingredients = new List<RecipeIngredient>();
                foreach (var ingredient in this.Ingredients)
                {
                    // legacy support
                    if (!string.IsNullOrEmpty(ingredient.Ingredient_Tag))
                        ingredient.SelectorValue = ingredient.Ingredient_Tag;
                    else if (ingredient.Ingredient_ItemID != 0)
                        ingredient.SelectorValue = ingredient.Ingredient_ItemID.ToString();

                    if (ingredient.Type == RecipeIngredient.ActionTypes.AddGenericIngredient)
                    {
                        var tag = CustomTags.GetTag(ingredient.SelectorValue);
                        if (tag == Tag.None)
                        {
                            SL.LogWarning("Could not get a tag by the name of '" + ingredient.SelectorValue);
                            return;
                        }

                        ingredients.Add(new RecipeIngredient
                        {
                            ActionType = ingredient.Type,
                            AddedIngredientType = new TagSourceSelector(tag)
                        });
                    }
                    else
                    {
                        int.TryParse(ingredient.SelectorValue, out int id);
                        if (id == 0)
                        {
                            SL.LogWarning("Picking an Ingredient based on Item ID, but no ID was set. Check your XML and make sure there are no logical errors. Aborting");
                            return;
                        }

                        var ingredientItem = ResourcesPrefabManager.Instance.GetItemPrefab(id);
                        if (!ingredientItem)
                        {
                            SL.Log("Error: Could not get ingredient id: " + id);
                            return;
                        }

                        // The item needs the station type tag in order to be used in a manual recipe on that station
                        var tag = TagSourceManager.GetCraftingIngredient(StationType);
                        if (!ingredientItem.HasTag(tag))
                        {
                            //SL.Log($"Adding tag {tag.TagName} to " + ingredientItem.name);

                            var comp = ingredientItem.GetComponent<TagSource>();
                            if (!comp)
                                comp = ingredientItem.gameObject.AddComponent<TagSource>();

                            comp.m_tagSelectors.Add(new TagSourceSelector(tag));
                        }

                        ingredients.Add(new RecipeIngredient()
                        {
                            ActionType = RecipeIngredient.ActionTypes.AddSpecificIngredient,
                            AddedIngredient = ingredientItem,
                        });
                    }
                }

                var recipe = ScriptableObject.CreateInstance<Recipe>();

                recipe.SetCraftingType(this.StationType);

                recipe.m_results = results.ToArray();
                recipe.SetRecipeIngredients(ingredients.ToArray());

                // set or generate UID
                if (string.IsNullOrEmpty(this.UID))
                {
                    var uid = $"{recipe.Results[0].ItemID}{recipe.Results[0].Quantity}";
                    foreach (var ing in recipe.Ingredients)
                    {
                        if (ing.AddedIngredient != null)
                            uid += $"{ing.AddedIngredient.ItemID}";
                        else if (ing.AddedIngredientType != null)
                            uid += $"{ing.AddedIngredientType.Tag.TagName}";
                    }
                    this.UID = uid;
                    recipe.m_uid = new UID(uid);
                }
                else
                    recipe.m_uid = new UID(this.UID);

                recipe.Init();

                // fix Recipe Manager dictionaries to contain our recipe
                var dict = References.ALL_RECIPES;
                var dict2 = References.RECIPES_PER_UTENSIL;

                if (dict.ContainsKey(recipe.UID))
                {
                    dict[recipe.UID] = recipe;
                }
                else
                {
                    dict.Add(recipe.UID, recipe);
                }

                if (!dict2.ContainsKey(recipe.CraftingStationType))
                {
                    dict2.Add(recipe.CraftingStationType, new List<UID>());
                }

                if (!dict2[recipe.CraftingStationType].Contains(recipe.UID))
                {
                    dict2[recipe.CraftingStationType].Add(recipe.UID);
                }

                m_applied = true;
            }
            catch (Exception e)
            {
                SL.LogWarning("Error applying recipe!");
                SL.LogInnerException(e);
            }
        }

        public static SL_Recipe ParseRecipe(Recipe recipe)
        {
            var recipeHolder = new SL_Recipe
            {
                //Name = recipe.Name,
                //RecipeID = recipe.RecipeID,
                StationType = recipe.CraftingStationType,
            };

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient.ActionType == RecipeIngredient.ActionTypes.AddSpecificIngredient)
                {
                    recipeHolder.Ingredients.Add(new Ingredient()
                    {
                        Type = ingredient.ActionType,
                        SelectorValue = ingredient.AddedIngredient.ItemID.ToString()
                    });
                }
                else
                {
                    recipeHolder.Ingredients.Add(new Ingredient()
                    {
                        Type = ingredient.ActionType,
                        SelectorValue = ingredient.AddedIngredientType.Tag.TagName
                    });
                }
            }

            foreach (ItemReferenceQuantity item in recipe.Results)
            {
                recipeHolder.Results.Add(new ItemQty
                {
                    ItemID = item.ItemID,
                    Quantity = item.Quantity
                });
            }

            return recipeHolder;
        }

        [SL_Serialized]
        public class Ingredient
        {
            public override string ToString()
            {
                string ret;
                if (Type == RecipeIngredient.ActionTypes.AddGenericIngredient)
                    ret = SelectorValue;
                else
                    if (!string.IsNullOrEmpty(this.SelectorValue))
                    ret = ResourcesPrefabManager.Instance.GetItemPrefab(this.SelectorValue)?.Name ?? "<not found>";
                else
                    ret = "<not set>";

                return ret;
            }

            public RecipeIngredient.ActionTypes Type;
            public string SelectorValue;

            // legacy
            ///<summary>[OBSOLETE] Use SelectorValue instead.</summary>
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public int Ingredient_ItemID;
            ///<summary>[OBSOLETE] Use SelectorValue instead.</summary>
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public string Ingredient_Tag;
            internal bool ShouldSerializeIngredient_ItemID() => false;
            internal bool ShouldSerializeIngredient_Tag() => false;
        }

        [SL_Serialized]
        public class ItemQty
        {
            public override string ToString()
                => $"{Quantity}x {ResourcesPrefabManager.Instance.GetItemPrefab(ItemID)?.Name ?? "<not found>"}";

            public int ItemID;
            public int Quantity;
        }
    }
}
