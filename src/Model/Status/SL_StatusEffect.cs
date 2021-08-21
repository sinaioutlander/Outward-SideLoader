using SideLoader.Helpers;
using SideLoader.Model;
using SideLoader.Model.Status;
using SideLoader.SLPacks;
using SideLoader.SLPacks.Categories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;

namespace SideLoader
{
    public class SL_StatusEffect : SL_StatusBase
    {
        #region IContentTemplate

        internal override bool Internal_IsCreatingNewID()
        {
            return !string.IsNullOrEmpty(this.StatusIdentifier) && this.StatusIdentifier != this.TargetStatusIdentifier;
        }

        internal override bool Internal_DoesTargetExist()
        {
            if (string.IsNullOrEmpty(this.TargetStatusIdentifier))
                return false;

            return ResourcesPrefabManager.Instance.GetStatusEffectPrefab(this.TargetStatusIdentifier);
        }

        internal override string Internal_DefaultTemplateName() => $"{AppliedID}";

        internal override object Internal_TargetID() => this.TargetStatusIdentifier;

        internal override object Internal_AppliedID() => string.IsNullOrEmpty(this.StatusIdentifier)
                                                         ? this.TargetStatusIdentifier
                                                         : this.StatusIdentifier;

        internal override object Internal_GetContent(object id)
        {
            if (string.IsNullOrEmpty((string)id))
                return null;

            References.RPM_STATUS_EFFECTS.TryGetValue((string)id, out StatusEffect ret);
            return ret;
        }

        internal override ContentTemplate Internal_ParseToTemplate(object content) => ParseStatusEffect((StatusEffect)content);

        internal override void Internal_ActualCreate() => Internal_Apply();

        #endregion

        // ~~~~~~~~~~~~~~~~~~~~

        /// <summary>Invoked when this template is applied during SideLoader's start or hot-reload.</summary>
        public event Action<StatusEffect> OnTemplateApplied;

        /// <summary> The StatusEffect you would like to clone from. Can also use TargetStatusID (checks for a Preset ID), but this takes priority.</summary>
        public string TargetStatusIdentifier;

        /// <summary>The new Preset ID for your Status Effect.</summary>
        public int NewStatusID = -1;
        /// <summary>The new Status Identifier name for your Status Effect. Used by ResourcesPrefabManager.GetStatusEffect(string identifier)</summary>
        public string StatusIdentifier;

        public string Name;
        public string Description;

        public float? Lifespan;
        public float? RefreshRate;

        public bool? Purgeable;

        public int? Priority;

        public bool? IgnoreBarrier;

        public float? BuildupRecoverySpeed;
        public bool? IgnoreBuildupIfApplied;

        public string AmplifiedStatusIdentifier;

        public string ComplicationStatusIdentifier;
        public string RequiredStatusIdentifier;
        public bool? RemoveRequiredStatus;

        public bool? NormalizeDamageDisplay;
        public bool? DisplayedInHUD;
        public bool? IsHidden;
        public bool? IsMalusEffect;

        public int? DelayedDestroyTime;

        public StatusEffect.ActionsOnHit? ActionOnHit;

        public StatusEffect.FamilyModes? FamilyMode;
        public SL_StatusEffectFamily BindFamily;
        public string ReferenceFamilyUID;

        //public string EffectTypeTag;
        public string[] Tags;

        public bool? PlayFXOnActivation;
        public Vector3? FXOffset;
        public StatusEffect.FXInstantiationTypes? VFXInstantiationType;
        public SL_PlayVFX.VFXPrefabs? VFXPrefab;
        public GlobalAudioManager.Sounds? SpecialSFX;
        public bool? PlaySpecialFXOnStop;

        public EditBehaviours EffectBehaviour = EditBehaviours.Override;
        public SL_EffectTransform[] Effects;

        [Obsolete("Use SL_StatusEffect.BindFamily or SL_StatusFamily instead")]
        [XmlIgnore] public StatusEffectFamily.LengthTypes? LengthType;

        [Obsolete("Use 'ApplyTemplate' instead (name change).")]
        public void Apply()
            => ApplyTemplate();

        internal void Internal_Apply()
        {
            if (string.IsNullOrEmpty(this.StatusIdentifier))
                this.StatusIdentifier = this.TargetStatusIdentifier;

            var status = CustomStatusEffects.CreateCustomStatus(this);

            Internal_ApplyTemplate(status);
            OnTemplateApplied?.Invoke(status);
        }

        internal virtual void Internal_ApplyTemplate(StatusEffect status)
        {
            SL.Log("Applying Status Effect template: " + Name ?? status.name);

            SLPackManager.AddLateApplyListener(OnLateApply, status);

            CustomStatusEffects.SetStatusLocalization(status, Name, Description);

            if (status.StatusData == null)
                status.StatusData = new StatusData();

            if (Lifespan != null)
                status.StatusData.LifeSpan = (float)Lifespan;

            if (RefreshRate != null)
                status.RefreshRate = (float)RefreshRate;

            if (this.Priority != null)
                status.m_priority = (int)this.Priority;

            if (this.Purgeable != null)
                status.m_purgeable = (bool)this.Purgeable;

            if (this.DelayedDestroyTime != null)
                status.DelayedDestroyTime = (int)this.DelayedDestroyTime;

            if (BuildupRecoverySpeed != null)
                status.BuildUpRecoverSpeed = (float)BuildupRecoverySpeed;

            if (IgnoreBuildupIfApplied != null)
                status.IgnoreBuildUpIfApplied = (bool)IgnoreBuildupIfApplied;

            if (DisplayedInHUD != null)
                status.DisplayInHud = (bool)DisplayedInHUD;

            if (IsHidden != null)
                status.IsHidden = (bool)IsHidden;

            if (IsMalusEffect != null)
                status.IsMalusEffect = (bool)this.IsMalusEffect;

            if (this.ActionOnHit != null)
                status.m_actionOnHit = (StatusEffect.ActionsOnHit)this.ActionOnHit;

            if (this.RemoveRequiredStatus != null)
                status.RemoveRequiredStatus = (bool)this.RemoveRequiredStatus;

            if (this.NormalizeDamageDisplay != null)
                status.NormalizeDamageDisplay = (bool)this.NormalizeDamageDisplay;

            if (this.IgnoreBarrier != null)
                status.IgnoreBarrier = (bool)this.IgnoreBarrier;

            if (this.StatusIdentifier != this.TargetStatusIdentifier)
                status.m_effectType = new TagSourceSelector(Tag.None);

            if (Tags != null)
                status.m_tagSource = (TagSource)CustomTags.SetTagSource(status.gameObject, Tags, true);
            else if (!status.GetComponent<TagSource>())
                status.m_tagSource = status.gameObject.AddComponent<TagSource>();

            if (this.PlayFXOnActivation != null)
                status.PlayFXOnActivation = (bool)this.PlayFXOnActivation;

            if (this.VFXInstantiationType != null)
                status.FxInstantiation = (StatusEffect.FXInstantiationTypes)this.VFXInstantiationType;

            if (this.VFXPrefab != null)
            {
                if (this.VFXPrefab == SL_PlayVFX.VFXPrefabs.NONE)
                    status.FXPrefab = null;
                else
                {
                    var clone = GameObject.Instantiate(SL_PlayVFX.GetVfxSystem((SL_PlayVFX.VFXPrefabs)this.VFXPrefab));
                    GameObject.DontDestroyOnLoad(clone);
                    status.FXPrefab = clone.transform;
                }
            }

            if (this.FXOffset != null)
                status.FxOffset = (Vector3)this.FXOffset;

            if (this.PlaySpecialFXOnStop != null)
                status.PlaySpecialFXOnStop = (bool)this.PlaySpecialFXOnStop;

            // setup family 
            if (this.FamilyMode == null && this.StatusIdentifier != this.TargetStatusIdentifier)
            {
                // Creating a new status, but no unique bind family was declared. Create one.
                var family = new StatusEffectFamily
                {
                    Name = this.StatusIdentifier + "_FAMILY",
                    LengthType = StatusEffectFamily.LengthTypes.Short,
                    MaxStackCount = 1,
                    StackBehavior = StatusEffectFamily.StackBehaviors.IndependantUnique
                };

                status.m_bindFamily = family;
                status.m_familyMode = StatusEffect.FamilyModes.Bind;
            }

            if (this.FamilyMode == StatusEffect.FamilyModes.Bind)
            {
                status.m_familyMode = StatusEffect.FamilyModes.Bind;
                if (this.BindFamily != null)
                    status.m_bindFamily = this.BindFamily.CreateAsBindFamily();
            }
            else if (this.FamilyMode == StatusEffect.FamilyModes.Reference)
            {
                status.m_familyMode = StatusEffect.FamilyModes.Reference;
                if (this.ReferenceFamilyUID != null)
                    status.m_stackingFamily = new StatusEffectFamilySelector() { SelectorValue = this.ReferenceFamilyUID };
            }

            // check for custom icon
            if (!string.IsNullOrEmpty(SerializedSLPackName) 
                && !string.IsNullOrEmpty(SerializedSubfolderName) 
                && SL.GetSLPack(SerializedSLPackName) is SLPack pack)
            {
                var dir = Path.Combine(pack.GetPathForCategory<StatusCategory>(), SerializedSubfolderName);

                if (pack.FileExists(dir, "icon.png"))
                {
                    var tex = pack.LoadTexture2D(dir, "icon.png");
                    var sprite = CustomTextures.CreateSprite(tex, CustomTextures.SpriteBorderTypes.NONE);

                    status.OverrideIcon = sprite;
                    status.m_defaultStatusIcon = new StatusTypeIcon(Tag.None) { Icon = sprite };
                }
            }
        }

        private void OnLateApply(object[] obj)
        {
            var status = obj[0] as StatusEffect;

            if (!status)
                return;

            LateApplyStatusEffects(status);
        }

        protected internal void LateApplyStatusEffects(StatusEffect status)
        {
            if (ComplicationStatusIdentifier != null)
            {
                if (!string.IsNullOrEmpty(ComplicationStatusIdentifier))
                {
                    var complicStatus = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(this.ComplicationStatusIdentifier);
                    if (complicStatus)
                        status.ComplicationStatus = complicStatus;
                }
                else
                {
                    status.ComplicationStatus = null;
                }
            }

            if (RequiredStatusIdentifier != null)
            {
                if (!string.IsNullOrEmpty(RequiredStatusIdentifier))
                {
                    var required = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(this.RequiredStatusIdentifier);
                    if (required)
                        status.RequiredStatus = required;
                }
                else
                {
                    status.RequiredStatus = null;
                }
            }

            if (AmplifiedStatusIdentifier != null)
            {
                if (!string.IsNullOrEmpty(this.AmplifiedStatusIdentifier))
                {
                    var amp = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(AmplifiedStatusIdentifier);
                    if (amp)
                        status.m_amplifiedStatus = amp;
                    else
                        SL.Log("StatusEffect.ApplyTemplate - could not find AmplifiedStatusIdentifier " + this.AmplifiedStatusIdentifier);
                }
                else
                    status.m_amplifiedStatus = null;
            }

            // setup signature and finalize

            if (EffectBehaviour == EditBehaviours.Destroy)
                UnityHelpers.DestroyChildren(status.transform);

            Transform signature;
            if (status.transform.childCount < 1)
            {
                signature = new GameObject($"SIGNATURE_{status.IdentifierName}").transform;
                signature.parent = status.transform;
                var comp = signature.gameObject.AddComponent<EffectSignature>();
                comp.SignatureUID = new UID($"{NewStatusID}_{status.IdentifierName}");
            }
            else
                signature = status.transform.GetChild(0);

            if (Effects != null)
            {
                SL_EffectTransform.ApplyTransformList(signature, Effects, EffectBehaviour);
            }

            // fix StatusData for the new effects
            CompileEffectsToData(status);

            var sigComp = signature.GetComponent<EffectSignature>();
            sigComp.enabled = true;

            var effects = signature.GetComponentsInChildren<Effect>();
            sigComp.Effects = effects.ToList();

            // Fix the effect signature for reference families
            if (status.FamilyMode == StatusEffect.FamilyModes.Reference)
            {
                signature.transform.parent = SL.CloneHolder;

                var family = status.EffectFamily;
                family.EffectSignature = sigComp;
                status.StatusData.EffectSignature = sigComp;
            }

            // Need to reset description after changing effects.
            status.RefreshLoc();
        }

        // Generate StatusData for the StatusEffect automatically
        public static void CompileEffectsToData(StatusEffect status)
        {
            // Get the EffectSignature component
            var signature = status.GetComponentInChildren<EffectSignature>();

            status.StatusData.EffectSignature = signature;

            // Get and Set the Effects list
            var effects = signature.GetComponentsInChildren<Effect>()?.ToList() ?? new List<Effect>();
            signature.Effects = effects;
            status.m_effectList = effects;

            // Finally, set the EffectsData[] array.
            status.StatusData.EffectsData = GenerateEffectsData(effects);

            // Not sure if this is needed or not, but I'm doing it to be extra safe.
            status.m_totalData = status.StatusData.EffectsData;
        }

        public static StatusData.EffectData[] GenerateEffectsData(List<Effect> effects, int level = 1)
        {
            // Create a list to generate the EffectData[] array into.
            var list = new List<StatusData.EffectData>();

            // Iterate over the effects
            foreach (var effect in effects)
            {
                // Create a blank holder, in the case this effect isn't supported or doesn't serialize anything.
                var data = new StatusData.EffectData()
                {
                    Data = new string[] { "" }
                };

                var type = effect.GetType();

                if (typeof(PunctualDamage).IsAssignableFrom(type))
                {
                    var comp = effect as PunctualDamage;

                    // For PunctualDamage, Data[0] is the entire damage, and Data[1] is the impact.

                    // Each damage goes "Damage : Type", and separated by a ';' char.
                    // So we just iterate over the damage and serialize like that.
                    var dmgString = "";
                    foreach (var dmg in comp.Damages)
                    {
                        if (dmgString != "")
                            dmgString += ";";

                        dmgString += $"{dmg.Damage * level}:{dmg.Type}";
                    }

                    // finally, set data
                    data.Data = new string[]
                    {
                        dmgString,
                        comp.Knockback.ToString()
                    };
                }
                else if (typeof(AffectNeed).IsAssignableFrom(type))
                {
                    var fi = At.GetFieldInfo(typeof(AffectNeed), "m_affectQuantity");
                    data.Data = new string[]
                    {
                        ((float)fi.GetValue(effect) * level).ToString()
                    };
                }
                // For most AffectStat components, the only thing that is serialized is the AffectQuantity.
                else if (At.GetFieldInfo(type, "AffectQuantity") is FieldInfo fi_AffectQuantity)
                {
                    data.Data = new string[]
                    {
                        ((float)fi_AffectQuantity.GetValue(effect) * level).ToString()
                    };
                }
                // AffectMana uses "Value" instead of AffectQuantity for some reason...
                else if (At.GetFieldInfo(type, "Value") is FieldInfo fi_Value)
                {
                    data.Data = new string[]
                    {
                        ((float)fi_Value.GetValue(effect) * level).ToString()
                    };
                }
                else // otherwise maybe I need to add support for this effect...
                {
                    //SL.Log("[StatusEffect] Unsupported effect: " + type, 1);
                }

                list.Add(data);

            }

            return list.ToArray();
        }

        public static SL_StatusEffect ParseStatusEffect(StatusEffect status)
        {
            // Status Effects dont serialize properly unless you instantiate a clone
            var clone = GameObject.Instantiate(status.gameObject).GetComponent<StatusEffect>();

            var type = Serializer.GetBestSLType(clone.GetType());
            var template = (SL_StatusEffect)Activator.CreateInstance(type);
            template.SerializeStatus(clone);

            // destroy the clone now that we're done with it
            GameObject.Destroy(clone);

            return template;
        }

        public virtual void SerializeStatus(StatusEffect status)
        {
            var preset = status.GetComponent<EffectPreset>();

            this.NewStatusID = preset?.PresetID ?? -1;
            this.TargetStatusIdentifier = status.IdentifierName;
            this.StatusIdentifier = status.IdentifierName;
            this.IgnoreBuildupIfApplied = status.IgnoreBuildUpIfApplied;
            this.BuildupRecoverySpeed = status.BuildUpRecoverSpeed;
            this.DisplayedInHUD = status.DisplayInHud;
            this.IsHidden = status.IsHidden;
            this.Lifespan = status.StatusData.LifeSpan;
            this.RefreshRate = status.RefreshRate;
            this.AmplifiedStatusIdentifier = status.AmplifiedStatus?.IdentifierName ?? "";
            this.PlayFXOnActivation = status.PlayFXOnActivation;
            this.ComplicationStatusIdentifier = status.ComplicationStatus?.IdentifierName;
            this.FXOffset = status.FxOffset;
            this.IgnoreBarrier = status.IgnoreBarrier;
            this.IsMalusEffect = status.IsMalusEffect;
            this.NormalizeDamageDisplay = status.NormalizeDamageDisplay;
            this.PlaySpecialFXOnStop = status.PlaySpecialFXOnStop;
            this.RemoveRequiredStatus = status.RemoveRequiredStatus;
            this.RequiredStatusIdentifier = status.RequiredStatus?.IdentifierName;
            this.SpecialSFX = status.SpecialSFX;
            this.VFXInstantiationType = status.FxInstantiation;

            this.ActionOnHit = status.ActionOnHit;

            this.Priority = status.m_priority;

            this.DelayedDestroyTime = status.DelayedDestroyTime;
            this.Purgeable = status.Purgeable;

            CustomStatusEffects.GetStatusLocalization(status, out Name, out Description);

            var tags = status.m_tagSource;
            if (tags)
                Tags = tags.Tags.Select(it => it.TagName).ToArray();

            var vfx = status.FXPrefab?.GetComponent<VFXSystem>();
            if (vfx)
                VFXPrefab = SL_PlayVFX.GetVFXSystemEnum(vfx);

            // PARSE EFFECT FAMILY
            FamilyMode = status.FamilyMode;
            if (status.EffectFamily != null)
            {
                if (FamilyMode == StatusEffect.FamilyModes.Bind)
                    BindFamily = SL_StatusEffectFamily.ParseEffectFamily(status.EffectFamily);
                else
                    ReferenceFamilyUID = status.EffectFamily.UID;
            }

            // For existing StatusEffects, the StatusData contains the real values, so we need to SetValue to each Effect.
            var statusData = status.StatusData.EffectsData;
            var components = status.GetComponentsInChildren<Effect>();
            for (int i = 0; i < components.Length; i++)
            {
                var comp = components[i];
                if (comp && comp.Signature.Length > 0)
                    comp.SetValue(statusData[i].Data);
            }

            var effects = new List<SL_EffectTransform>();
            Transform signature;
            if (status.transform.childCount > 0)
            {
                signature = status.transform.GetChild(0);
                if (signature.transform.childCount > 0)
                {
                    foreach (Transform child in signature.transform)
                    {
                        var effectsChild = SL_EffectTransform.ParseTransform(child);

                        if (effectsChild.HasContent)
                            effects.Add(effectsChild);
                    }
                }
            }

            Effects = effects.ToArray();
        }

        public override void ExportIcons(Component comp, string folder)
        {
            base.ExportIcons(comp, folder);

            var status = comp as StatusEffect;

            if (status.StatusIcon)
                CustomTextures.SaveIconAsPNG(status.StatusIcon, folder);

            if (comp is LevelStatusEffect levelComp)
            {
                for (int i = 0; i < levelComp.StatusLevelData.Length; i++)
                {
                    var data = levelComp.StatusLevelData[i];
                    if (data != null && data.Icon)
                        CustomTextures.SaveIconAsPNG(data.Icon, folder, $"icon{i + 2}");
                }
            }
        }
    }
}
