using SideLoader.Model;
using SideLoader.Model.Status;
using SideLoader.SLPacks;
using SideLoader.SLPacks.Categories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace SideLoader
{
    public class SL_ImbueEffect : SL_StatusBase, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_ImbueEffect);
        public Type GameModel => typeof(ImbueEffectPreset);

        #region IContentTemplate

        internal override bool Internal_IsCreatingNewID() => this.NewStatusID != -1 && NewStatusID != TargetStatusID;

        internal override bool Internal_DoesTargetExist() => ResourcesPrefabManager.Instance.GetEffectPreset(this.TargetStatusID);

        internal override string Internal_DefaultTemplateName() => $"{AppliedID}_{Name}";

        internal override object Internal_TargetID() => TargetStatusID;

        internal override object Internal_AppliedID() => NewStatusID == -1
                                                            ? TargetStatusID
                                                            : NewStatusID;

        internal override object Internal_GetContent(object id)
        {
            if (string.IsNullOrEmpty((string)id))
                return null;

            if (int.TryParse((string)id, out int result))
            {
                References.RPM_EFFECT_PRESETS.TryGetValue(result, out EffectPreset ret);
                return (ImbueEffectPreset)ret;
            }
            else
                return null;
        }

        internal override ContentTemplate Internal_ParseToTemplate(object content)
            => ParseImbueEffect((ImbueEffectPreset)content);

        internal override void Internal_ActualCreate() => Internal_Apply();

        #endregion

        /// <summary>Invoked when this template is applied during SideLoader's start or hot-reload.</summary>
        public event Action<ImbueEffectPreset> OnTemplateApplied;

        /// <summary> [NOT SERIALIZED] The name of the SLPack this custom item template comes from (or is using).
        /// If defining from C#, you can set this to the name of the pack you want to load assets from.</summary>
        [XmlIgnore]
        public string SLPackName;
        /// <summary> [NOT SERIALIZED] The name of the folder this custom item is using for textures (MyPack/Items/[SubfolderName]/Textures/).</summary>
        [XmlIgnore]
        public string SubfolderName;

        /// <summary>This is the Preset ID of the Status Effect you want to base from.</summary>
        public int TargetStatusID;
        /// <summary>The new Preset ID for your Status Effect</summary>
        public int NewStatusID = -1;

        public string Name;
        public string Description;

        public EditBehaviours EffectBehaviour = EditBehaviours.Override;
        public SL_EffectTransform[] Effects;

        [Obsolete("Use 'ApplyTemplate' instead (name change).")]
        public void Apply()	
            => Internal_ApplyTemplate();

        private void Internal_Apply()
        {
            var imbue = Internal_ApplyTemplate();
            this.OnTemplateApplied?.Invoke(imbue);
        }

        internal ImbueEffectPreset Internal_ApplyTemplate()
        {
            if (this.NewStatusID == -1)
                this.NewStatusID = this.TargetStatusID;

            var preset = CustomStatusEffects.CreateCustomImbue(this);

            SLPackManager.AddLateApplyListener(OnLateApply, preset);

            CustomStatusEffects.SetImbueLocalization(preset, Name, Description);

            // check for custom icon
            if (!string.IsNullOrEmpty(SLPackName) && !string.IsNullOrEmpty(SubfolderName) && SL.GetSLPack(SLPackName) is SLPack pack)
            {
                var dir = Path.Combine(pack.GetPathForCategory<StatusCategory>(), SubfolderName);

                if (pack.FileExists(dir, "icon.png"))
                {
                    var tex = pack.LoadTexture2D(dir, "icon.png");
                    var sprite = CustomTextures.CreateSprite(tex, CustomTextures.SpriteBorderTypes.NONE);

                    preset.ImbueStatusIcon = sprite;
                }
            }

            return preset;
        }

        private void OnLateApply(object[] obj)
        {
            var preset = obj[0] as ImbueEffectPreset;

            SL_EffectTransform.ApplyTransformList(preset.transform, Effects, EffectBehaviour);
        }

        public static SL_ImbueEffect ParseImbueEffect(ImbueEffectPreset imbue)
        {
            var template = new SL_ImbueEffect
            {
                TargetStatusID = imbue.PresetID,
                Name = imbue.Name,
                Description = imbue.Description
            };

            //CustomStatusEffects.GetImbueLocalization(imbue, out template.Name, out template.Description);

            var list = new List<SL_EffectTransform>();
            foreach (Transform child in imbue.transform)
            {
                var effectsChild = SL_EffectTransform.ParseTransform(child);

                if (effectsChild.HasContent)
                {
                    list.Add(effectsChild);
                }
            }
            template.Effects = list.ToArray();

            return template;
        }

        public override void ExportIcons(Component comp, string folder)
        {
            base.ExportIcons(comp, folder);

            var imbue = comp as ImbueEffectPreset;

            if (imbue.ImbueStatusIcon)
                CustomTextures.SaveIconAsPNG(imbue.ImbueStatusIcon, folder);
        }
    }
}
