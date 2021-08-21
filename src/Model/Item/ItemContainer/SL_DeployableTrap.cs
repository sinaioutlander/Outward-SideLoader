using SideLoader.SLPacks;
using System;
using System.Collections.Generic;

namespace SideLoader
{
    public class SL_DeployableTrap : SL_ItemContainer
    {
        public bool? OneTimeUse;
        public SL_TrapEffectRecipe[] TrapRecipeEffects;

        public override void ApplyToItem(Item item)
        {
            base.ApplyToItem(item);

            var trap = item as DeployableTrap;

            if (this.OneTimeUse != null)
                trap.m_oneTimeUse = (bool)this.OneTimeUse;
           
            if (this.TrapRecipeEffects != null)
            {
                var list = new List<TrapEffectRecipe>();
                foreach (var holder in this.TrapRecipeEffects)
                    list.Add(holder.Apply());
                trap.m_trapRecipes = list.ToArray();
            }
        }

        public override void SerializeItem(Item item)
        {
            base.SerializeItem(item);

            var trap = item as DeployableTrap;

            OneTimeUse = trap.m_oneTimeUse;

            var recipes = trap.m_trapRecipes;
            if (recipes != null)
            {
                var list = new List<SL_TrapEffectRecipe>();
                foreach (var recipe in recipes)
                {
                    var dmRecipe = new SL_TrapEffectRecipe();
                    dmRecipe.Serialize(recipe);
                    list.Add(dmRecipe);
                }
                this.TrapRecipeEffects = list.ToArray();
            }
        }
    }
}
