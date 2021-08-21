using SideLoader.Model;
using SideLoader.SLPacks;
using SideLoader.SLPacks.Categories;
using System;
using System.Linq;
using System.Xml.Serialization;

namespace SideLoader
{
    [SL_Serialized]
    public class SL_StatusEffectFamily : ContentTemplate
    {
        #region IContentTemplate

        public override ITemplateCategory PackCategory => SLPackManager.GetCategoryInstance<StatusFamilyCategory>();
        public override bool TemplateAllowedInSubfolder => false;
        public override string DefaultTemplateName => this.UID ?? "Untitled StatusFamily";

        public override void ApplyActualTemplate() => Internal_ApplyTemplate();

        #endregion

        public string UID;
        public string Name;

        public StatusEffectFamily.StackBehaviors? StackBehaviour;
        public int? MaxStackCount = -1;

        public StatusEffectFamily.LengthTypes? LengthType;

        [Obsolete("Use 'ApplyTemplate' instead (name change).")]
        public void Apply()
            => ApplyTemplate();

        // used by SL_StatusEffect Bind Families
        public StatusEffectFamily CreateAsBindFamily()
        {
            var ret = new StatusEffectFamily
            {
                Name = this.Name,
                LengthType = (StatusEffectFamily.LengthTypes)this.LengthType,
                MaxStackCount = (int)this.MaxStackCount,
                StackBehavior = (StatusEffectFamily.StackBehaviors)this.StackBehaviour
            };

            ret.m_uid = new UID(this.UID);

            return ret;
        }

        // Normal template apply method
        internal void Internal_ApplyTemplate()
        {
            if (!StatusEffectFamilyLibrary.Instance)
                return;

            var library = StatusEffectFamilyLibrary.Instance;

            StatusEffectFamily family;
            if (library.StatusEffectFamilies.Where(it => (string)it.UID == this.UID).Any())
            {
                family = library.StatusEffectFamilies.First(it => (string)it.UID == this.UID);
            }
            else
            {
                family = new StatusEffectFamily();
                library.StatusEffectFamilies.Add(family);
            }

            if (family == null)
            {
                SL.LogWarning("Applying SL_StatusEffectFamily template, null error");
                return;
            }

            if (this.UID != null)
                family.m_uid = new UID(this.UID);

            if (this.Name != null)
                family.Name = this.Name;

            if (this.StackBehaviour != null)
                family.StackBehavior = (StatusEffectFamily.StackBehaviors)this.StackBehaviour;

            if (this.MaxStackCount != null)
                family.MaxStackCount = (int)this.MaxStackCount;

            if (this.LengthType != null)
                family.LengthType = (StatusEffectFamily.LengthTypes)this.LengthType;
        }

        internal static SL_StatusEffectFamily ParseEffectFamily(StatusEffectFamily family)
        {
            return new SL_StatusEffectFamily
            {
                UID = family.UID,
                Name = family.Name,
                LengthType = family.LengthType,
                MaxStackCount = family.MaxStackCount,
                StackBehaviour = family.StackBehavior
            };
        }
    }
}
