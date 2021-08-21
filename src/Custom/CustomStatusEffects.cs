using SideLoader.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace SideLoader
{
    /// <summary>
    /// SideLoader's manager class for Custom Status Effects. Contains helpful methods for creating and managing SL_StatusEffects and SL_ImbueEffects.
    /// </summary>
    public class CustomStatusEffects
    {
        /// <summary>Cached un-edited Status Effects.</summary>
        public static readonly Dictionary<string, StatusEffect> OrigStatusEffects = new Dictionary<string, StatusEffect>();

        /// <summary>Cached un-edited Effect Presets, used by Imbue Presets. For StatusEffects, use GetOrigStatusEffect.</summary>
        public static readonly Dictionary<int, EffectPreset> OrigEffectPresets = new Dictionary<int, EffectPreset>();

        // ================== HELPERS ==================

        /// <summary>
        /// Helper to get the cached ORIGINAL (not modified) EffectPreset of this PresetID.
        /// </summary>
        /// <param name="presetID">The Preset ID of the effect preset you want.</param>
        /// <returns>The EffectPreset, if found.</returns>
        public static EffectPreset GetOrigEffectPreset(int presetID)
        {
            if (OrigEffectPresets.ContainsKey(presetID))
                return OrigEffectPresets[presetID];
            else
                return ResourcesPrefabManager.Instance.GetEffectPreset(presetID);
        }

        /// <summary>
        /// Get the original Status Effect with this identifier.
        /// </summary>
        /// <param name="identifier">The identifier to get.</param>
        /// <returns>The EffectPreset, if found, otherwise null.</returns>
        public static StatusEffect GetOrigStatusEffect(string identifier)
        {
            if (OrigStatusEffects.ContainsKey(identifier))
                return OrigStatusEffects[identifier];
            else
                return ResourcesPrefabManager.Instance.GetStatusEffectPrefab(identifier);
        }

        /// <summary>
        /// Use this to create or modify a Status Effect.
        /// </summary>
        /// <param name="template">The SL_StatusEffect template.</param>
        /// <returns>The new or existing StatusEffect.</returns>
        public static StatusEffect CreateCustomStatus(SL_StatusEffect template)
        {
            var original = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(template.TargetStatusIdentifier);

            if (!original)
            {
                SL.Log($"CreateCustomStatus - Could not find any status with the Identifier '{template.TargetStatusIdentifier}' (or none was set).");
                return null;
            }

            var preset = original.GetComponent<EffectPreset>();
            if (!preset && template.NewStatusID < -1 || template.NewStatusID > 0)
            {
                preset = original.gameObject.AddComponent<EffectPreset>();
                preset.m_StatusEffectID = template.NewStatusID;
            }

            StatusEffect status;

            if (template.TargetStatusIdentifier == template.StatusIdentifier)
            {
                // editing orig status
                if (!OrigStatusEffects.ContainsKey(template.TargetStatusIdentifier))
                {
                    // instantiate and cache original
                    var cached = GameObject.Instantiate(original.gameObject).GetComponent<StatusEffect>();
                    cached.gameObject.SetActive(false);
                    GameObject.DontDestroyOnLoad(cached.gameObject);
                    OrigStatusEffects.Add(template.TargetStatusIdentifier, cached);
                }

                status = original;
            }
            else
            {
                // instantiate original and use that as newEffect
                status = GameObject.Instantiate(original.gameObject).GetComponent<StatusEffect>();
                status.gameObject.SetActive(false);

                // Set Status identifier
                status.m_identifierName = template.StatusIdentifier;

                if (preset && (template.NewStatusID < -1 || template.NewStatusID > 0))
                    preset.m_StatusEffectID = template.NewStatusID;

                // Fix localization
                GetStatusLocalization(original, out string name, out string desc);
                SetStatusLocalization(status, name, desc);

                // Fix status data and stack
                status.m_statusStack = null;
                status.m_amplifiedStatus = null;
            }

            var gameType = Serializer.GetGameType(template.GetType());
            if (gameType != status.GetType())
            {
                status = (StatusEffect)UnityHelpers.FixComponentType(gameType, status);
            }

            int presetID = status.GetComponent<EffectPreset>()?.PresetID ?? -1;
            var id = "";
            if (presetID < -1 || presetID > 0)
                id += presetID + "_";
            status.gameObject.name = id + status.IdentifierName;

            // fix RPM_STATUS_EFFECTS dictionary
            if (!References.RPM_STATUS_EFFECTS.ContainsKey(status.IdentifierName))
                References.RPM_STATUS_EFFECTS.Add(status.IdentifierName, status);
            else
                References.RPM_STATUS_EFFECTS[status.IdentifierName] = status;

            // fix RPM_Presets dictionary
            if (template.NewStatusID < -1 || template.NewStatusID > 0)
            {
                if (!References.RPM_EFFECT_PRESETS.ContainsKey(template.NewStatusID))
                    References.RPM_EFFECT_PRESETS.Add(template.NewStatusID, status.GetComponent<EffectPreset>());
                else
                    References.RPM_EFFECT_PRESETS[template.NewStatusID] = status.GetComponent<EffectPreset>();
            }

            // Always do this
            GameObject.DontDestroyOnLoad(status.gameObject);

            return status;
        }

        /// <summary>
        /// Get the Localization for the Status Effect (name and description).
        /// </summary>
        /// <param name="effect">The Status Effect to get localization for.</param>
        /// <param name="name">The output name.</param>
        /// <param name="desc">The output description.</param>
        public static void GetStatusLocalization(StatusEffect effect, out string name, out string desc)
        {
            name = LocalizationManager.Instance.GetLoc(effect.m_nameLocKey);
            desc = LocalizationManager.Instance.GetLoc(effect.m_descriptionLocKey);
        }

        /// <summary>
        /// Helper to set the Name and Description localization for a StatusEffect
        /// </summary>
        public static void SetStatusLocalization(StatusEffect effect, string name, string description)
        {
            GetStatusLocalization(effect, out string oldName, out string oldDesc);

            if (string.IsNullOrEmpty(name))
                name = oldName;
            if (string.IsNullOrEmpty(description))
                description = oldDesc;

            var nameKey = $"NAME_{effect.IdentifierName}";
            effect.m_nameLocKey = nameKey;

            if (References.GENERAL_LOCALIZATION.ContainsKey(nameKey))
                References.GENERAL_LOCALIZATION[nameKey] = name;
            else
                References.GENERAL_LOCALIZATION.Add(nameKey, name);

            var descKey = $"DESC_{effect.IdentifierName}";
            effect.m_descriptionLocKey = descKey;

            if (References.GENERAL_LOCALIZATION.ContainsKey(descKey))
                References.GENERAL_LOCALIZATION[descKey] = description;
            else
                References.GENERAL_LOCALIZATION.Add(descKey, description);
        }

        /// <summary>
        /// Use this to create or modify an Imbue Effect status.
        /// </summary>
        /// <param name="template">The SL_ImbueEffect Template for this imbue.</param>
        public static ImbueEffectPreset CreateCustomImbue(SL_ImbueEffect template)
        {
            var original = (ImbueEffectPreset)GetOrigEffectPreset(template.TargetStatusID);

            if (!original)
            {
                SL.LogError("Could not find an ImbueEffectPreset with the Preset ID " + template.TargetStatusID);
                return null;
            }

            ImbueEffectPreset newEffect;

            if (template.TargetStatusID == template.NewStatusID)
            {
                if (!OrigEffectPresets.ContainsKey(template.TargetStatusID))
                {
                    // instantiate and cache original
                    var cached = GameObject.Instantiate(original.gameObject).GetComponent<EffectPreset>();
                    cached.gameObject.SetActive(false);
                    GameObject.DontDestroyOnLoad(cached.gameObject);
                    OrigEffectPresets.Add(template.TargetStatusID, cached);
                }

                newEffect = original;
            }
            else
            {
                // instantiate original and use that as newEffect
                newEffect = GameObject.Instantiate(original.gameObject).GetComponent<ImbueEffectPreset>();
                newEffect.gameObject.SetActive(false);

                // Set Preset ID
                newEffect.m_StatusEffectID = template.NewStatusID;

                // Fix localization
                GetImbueLocalization(original, out string name, out string desc);
                SetImbueLocalization(newEffect, name, desc);
            }

            newEffect.gameObject.name = template.NewStatusID + "_" + (template.Name ?? newEffect.Name);

            // fix RPM_Presets dictionary
            if (!References.RPM_EFFECT_PRESETS.ContainsKey(template.NewStatusID))
                References.RPM_EFFECT_PRESETS.Add(template.NewStatusID, newEffect.GetComponent<EffectPreset>());
            else
                References.RPM_EFFECT_PRESETS[template.NewStatusID] = newEffect;

            // Always do this
            GameObject.DontDestroyOnLoad(newEffect.gameObject);

            //// Apply template
            //if (SL.PacksLoaded)
            //    template.ApplyTemplate();
            //else
            //    SL.INTERNAL_ApplyStatuses += template.ApplyTemplate;

            return newEffect;
        }

        /// <summary>
        /// Helper to get the name and description for an Imbue.
        /// </summary>
        /// <param name="preset">The Imbue Preset to get localization for.</param>
        /// <param name="name">The output name.</param>
        /// <param name="desc">The output description.</param>
        public static void GetImbueLocalization(ImbueEffectPreset preset, out string name, out string desc)
        {
            name = preset.Name;
            desc = preset.Description;
        }

        /// <summary>
        /// Helper to set the Name and Description localization for an Imbue Preset
        /// </summary>
        public static void SetImbueLocalization(ImbueEffectPreset preset, string name, string description)
        {
            GetImbueLocalization(preset, out string oldName, out string oldDesc);

            if (string.IsNullOrEmpty(name))
                name = oldName;

            if (string.IsNullOrEmpty(description))
                description = oldDesc;

            var nameKey = $"NAME_{preset.PresetID}_{preset.Name.Trim()}";
            preset.m_imbueNameKey = nameKey;

            if (References.GENERAL_LOCALIZATION.ContainsKey(nameKey))
                References.GENERAL_LOCALIZATION[nameKey] = name;
            else
                References.GENERAL_LOCALIZATION.Add(nameKey, name);

            var descKey = $"DESC_{preset.PresetID}_{preset.Name.Trim()}";
            preset.m_imbueDescKey = descKey;

            if (References.GENERAL_LOCALIZATION.ContainsKey(descKey))
                References.GENERAL_LOCALIZATION[descKey] = description;
            else
                References.GENERAL_LOCALIZATION.Add(descKey, description);

            if (preset.GetComponent<StatusEffect>() is StatusEffect status)
                SetStatusLocalization(status, name, description);
        }
    }
}
