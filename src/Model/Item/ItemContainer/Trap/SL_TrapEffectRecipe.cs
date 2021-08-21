using SideLoader.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace SideLoader
{
    [SL_Serialized]
    public class SL_TrapEffectRecipe
    {
        public string Name;
        public string Description;
        public bool ReplaceBasicEffects;

        public List<string> CompatibleItemTags;
        public List<int> CompatibleItemIDs;

        public List<SL_Effect> TrapEffects;
        public List<SL_Effect> HiddenEffects;

        public void SetLocalization()
        {
            var dict = References.GENERAL_LOCALIZATION;

            var descKey = $"{Name}_Desc";

            if (dict.ContainsKey(Name))
                dict[Name] = Name;
            else
                dict.Add(Name, Name);

            if (dict.ContainsKey(descKey))
                dict[descKey] = Description;
            else
                dict.Add(descKey, Description);
        }

        public TrapEffectRecipe Apply()
        {
            var recipe = new TrapEffectRecipe();

            recipe.m_name = this.Name;

            SetLocalization();

            recipe.ReplaceBasicEffects = this.ReplaceBasicEffects;

            if (this.CompatibleItemTags != null)
            {
                var list = new List<TagSourceSelector>();
                foreach (var tagName in this.CompatibleItemTags)
                {
                    if (CustomTags.GetTag(tagName) is Tag tag && tag != Tag.None)
                        list.Add(new TagSourceSelector(tag));
                }
                recipe.m_compatibleTags = list.ToArray();
            }

            if (this.CompatibleItemIDs != null)
            {
                var list = new List<Item>();
                foreach (var itemID in this.CompatibleItemIDs)
                {
                    if (ResourcesPrefabManager.Instance.GetItemPrefab(itemID) is Item item)
                        list.Add(item);
                }
                recipe.m_compatibleItems = list.ToArray();
            }

            if (this.TrapEffects != null && this.TrapEffects.Count > 0)
            {
                var prefab = recipe.TrapEffectsPrefab;
                if (!prefab)
                {
                    prefab = new GameObject($"{this.Name}_NormalEffects").transform;
                    GameObject.DontDestroyOnLoad(prefab.gameObject);
                    recipe.TrapEffectsPrefab = prefab;
                }
                UnityHelpers.DestroyChildren(prefab);

                foreach (var effect in this.TrapEffects)
                    effect.ApplyToTransform(prefab);
            }

            if (this.HiddenEffects != null && this.HiddenEffects.Count > 0)
            {
                var prefab = recipe.HiddenTrapEffectsPrefab;
                if (!prefab)
                {
                    prefab = new GameObject($"{this.Name}_HiddenEffects").transform;
                    GameObject.DontDestroyOnLoad(prefab.gameObject);
                    recipe.HiddenTrapEffectsPrefab = prefab;
                }
                UnityHelpers.DestroyChildren(prefab);

                foreach (var effect in this.HiddenEffects)
                    effect.ApplyToTransform(prefab);
            }

            return recipe;
        }

        public void Serialize(TrapEffectRecipe recipe)
        {
            this.Name = recipe.Name;
            this.Description = recipe.Description;

            var items = recipe.m_compatibleItems;
            if (items != null)
            {
                this.CompatibleItemIDs = new List<int>();
                foreach (var item in items)
                    this.CompatibleItemIDs.Add(item.ItemID);
            }

            var tags = recipe.m_compatibleTags;
            if (tags != null)
            {
                this.CompatibleItemTags = new List<string>();
                foreach (var tag in tags)
                    this.CompatibleItemTags.Add(tag.Tag.TagName);
            }

            if (recipe.TrapEffectsPrefab)
            {
                this.TrapEffects = new List<SL_Effect>();
                foreach (var effect in recipe.TrapEffectsPrefab.GetComponents<Effect>())
                    this.TrapEffects.Add(SL_Effect.ParseEffect(effect));
            }
            if (recipe.HiddenTrapEffectsPrefab)
            {
                this.HiddenEffects = new List<SL_Effect>();
                foreach (var effect in recipe.HiddenTrapEffectsPrefab.GetComponents<Effect>())
                    this.HiddenEffects.Add(SL_Effect.ParseEffect(effect));
            }
        }

        public override string ToString()
        {
            return $"{this.Name}";
        }
    }
}
