using SideLoader.Helpers;
using SideLoader.Model;
using SideLoader.SLPacks;
using SideLoader.SLPacks.Categories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace SideLoader
{
    [SL_Serialized]
    public class SL_Item : ContentTemplate
    {
        #region IContentTemplate

        public override ITemplateCategory PackCategory => SLPackManager.GetCategoryInstance<ItemCategory>();
        public override string DefaultTemplateName => $"{this.AppliedID}_{this.Name}";

        public override object TargetID => this.Target_ItemID;
        public override object AppliedID => New_ItemID != -1 ? this.New_ItemID  : this.Target_ItemID;

        //public override bool IsCreatingNewID => this.New_ItemID != -1 && this.New_ItemID != this.Target_ItemID;
        public override bool DoesTargetExist => ResourcesPrefabManager.Instance.GetItemPrefab(this.Target_ItemID);

        public override bool TemplateAllowedInSubfolder => true;

        public override bool CanParseContent => true;
        public override ContentTemplate ParseToTemplate(object content) => ParseItemToTemplate(content as Item);

        [XmlIgnore]
        public override string SerializedSLPackName
        {
            get => SLPackName;
            set => SLPackName = value;
        }
        [XmlIgnore]
        public override string SerializedSubfolderName
        {
            get => SubfolderName;
            set => SubfolderName = value;
        }
        [XmlIgnore]
        public override string SerializedFilename
        {
            get => m_serializedFilename;
            set => m_serializedFilename = value;
        }

        public override void ApplyActualTemplate() => Internal_Create();

        public override object GetContentFromID(object id)
        {
            if (string.IsNullOrEmpty((string)id))
                return null;

            References.RPM_ITEM_PREFABS.TryGetValue((string)id, out Item ret);
            return ret;
        }

        #endregion

        // ~~~~~~~~~~~~~~ Events ~~~~~~~~~~~~~~

        internal static readonly Dictionary<int, List<Action<Item>>> s_initCallbacks = new Dictionary<int, List<Action<Item>>>();

        /// <summary>
        /// Add a listener to the OnInstanceStart event, which is called when an Item with this template's applied ID is created or loaded during gameplay.
        /// </summary>
        /// <param name="listener">Your callback. The Item argument is the Item instance.</param>
        public void AddOnInstanceStartListener(Action<Item> listener)
            => AddOnInstanceStartListener((int)this.AppliedID, listener);

        /// <summary>
        /// Add a listener to the OnInstanceStart event, which is called for items when they are created during gameplay.
        /// </summary>
        /// <param name="itemID">The Item ID to listen for.</param>
        /// <param name="listener">Your callback. The Item argument is the Item instance.</param>
        public static void AddOnInstanceStartListener(int itemID, Action<Item> listener)
        {
            if (s_initCallbacks.ContainsKey(itemID))
                s_initCallbacks[itemID].Add(listener);
            else
                s_initCallbacks.Add(itemID, new List<Action<Item>> { listener });
        }

        /// <summary>Invoked when this template is applied during SideLoader's start or hot-reload.</summary>
        public event Action<Item> OnTemplateApplied;

        /// <summary>If SL has loaded the template, this should point to the reference prefab that was created for it.</summary>
        public Item CurrentPrefab => m_item;
        protected internal Item m_item;

        // ~~~~~~~~~~~~~~ Actual Template ~~~~~~~~~~~~~~

        internal Dictionary<string, SL_Material> m_serializedMaterials = new Dictionary<string, SL_Material>();

        /// <summary> [NOT SERIALIZED] The name of the SLPack this custom item template comes from (or is using).
        /// If defining from C#, you can set this to the name of the pack you want to load assets from.</summary>
        [XmlIgnore] public string SLPackName;
        /// <summary> [NOT SERIALIZED] The name of the folder this custom item is using for textures (MyPack/Items/[SubfolderName]/Textures/).</summary>
        [XmlIgnore] public string SubfolderName;
        internal string m_serializedFilename;

        // [XmlIgnore] public virtual bool ShouldApplyLate => false;

        /// <summary>The Item ID of the Item you are cloning FROM</summary>
        public int Target_ItemID = -1;
        /// <summary>The NEW Item ID for your custom Item (can be the same as target, will overwrite)</summary>
        public int New_ItemID = -1;

        public string Name;
        public string Description;

        /// <summary>The Item ID of the Legacy Item (the upgrade of this item when placed in a Legacy Chest)</summary>
        public int? LegacyItemID;

        /// <summary>Can the item be picked up?</summary>
        public bool? IsPickable;
        /// <summary>Can you "Use" the item? ("Use" option from menu)</summary>
        public bool? IsUsable;
        public int? QtyRemovedOnUse;
        public bool? GroupItemInDisplay;
        public bool? HasPhysicsWhenWorld;
        public bool? RepairedInRest;
        public Item.BehaviorOnNoDurabilityType? BehaviorOnNoDurability;

        public Character.SpellCastType? CastType;
        public Character.SpellCastModifier? CastModifier;
        public bool? CastLocomotionEnabled;
        public float? MobileCastMovementMult;
        public int? CastSheatheRequired;

        public float? OverrideSellModifier;

        /// <summary>Item Tags, represented as strings (uses CustomTags.GetTag(string tagName)).</summary>
        public string[] Tags;

        /// <summary>Holder for the ItemStats object</summary>
        public SL_ItemStats StatsHolder;

        /// <summary>Determines how the ItemExtensions are replaced and edited</summary>
        public EditBehaviours ExtensionsEditBehaviour = EditBehaviours.NONE;
        /// <summary>List of SL_ItemExtensions for this item. Can only have one per item.</summary>
        public SL_ItemExtension[] ItemExtensions;

        ///// <summary>Determines how the EffectTransforms are replaced and edited</summary>
        public EditBehaviours EffectBehaviour = EditBehaviours.Override;
        /// <summary>Transform heirarchy containing the Effects and EffectConditions</summary>
        public SL_EffectTransform[] EffectTransforms;

        // Visual prefab stuff
        public SL_ItemVisual ItemVisuals;
        public SL_ItemVisual SpecialItemVisuals;
        public SL_ItemVisual SpecialFemaleItemVisuals;

        [Obsolete("Use 'ApplyTemplate' instead (name change).")]
        public void Apply()
            => ApplyTemplate();

        internal void Internal_Create()
        {
            if (this.New_ItemID == -1)
                this.New_ItemID = this.Target_ItemID;

            var item = CustomItems.CreateCustomItem(this);

            ApplyToItem(item);
            item.IsPrefab = true;

            m_item = item;

            try
            {
                this.OnTemplateApplied?.Invoke(item);
            }
            catch (Exception e)
            {
                SL.LogWarning("Exception invoking OnTemplateApplied!");
                SL.LogInnerException(e);
            }
        }

        /// <summary>
        /// Applies the template immediately to the provided Item. Calling this directly will NOT invoke OnTemplateApplied.
        /// </summary>
        public virtual void ApplyToItem(Item item)
        {
            SL.Log("Applying Item Template. ID: " + New_ItemID + ", Name: " + (Name ?? item.Name));

            var goName = Name ?? item.Name;
            goName = Serializer.ReplaceInvalidChars(goName);
            item.gameObject.name = $"{New_ItemID}_{goName}";

            // re-set this, just to be safe. The component might have been replaced by FixComponentTypeIfNeeded.
            CustomItems.SetItemID(New_ItemID, item);

            CustomItems.SetNameAndDescription(item, this.Name ?? item.Name, this.Description ?? item.Description);

            if (this.LegacyItemID != null)
                item.LegacyItemID = (int)this.LegacyItemID;

            if (this.CastLocomotionEnabled != null)
                item.CastLocomotionEnabled = (bool)this.CastLocomotionEnabled;

            if (this.CastModifier != null)
                item.CastModifier = (Character.SpellCastModifier)this.CastModifier;

            if (this.CastSheatheRequired != null)
                item.CastSheathRequired = (int)this.CastSheatheRequired;

            if (this.GroupItemInDisplay != null)
                item.GroupItemInDisplay = (bool)this.GroupItemInDisplay;

            if (this.HasPhysicsWhenWorld != null)
                item.HasPhysicsWhenWorld = (bool)this.HasPhysicsWhenWorld;

            if (this.IsPickable != null)
                item.IsPickable = (bool)this.IsPickable;

            if (this.IsUsable != null)
                item.IsUsable = (bool)this.IsUsable;

            if (this.QtyRemovedOnUse != null)
                item.QtyRemovedOnUse = (int)this.QtyRemovedOnUse;

            if (this.MobileCastMovementMult != null)
                item.MobileCastMovementMult = (float)this.MobileCastMovementMult;

            if (this.RepairedInRest != null)
                item.RepairedInRest = (bool)this.RepairedInRest;

            if (this.BehaviorOnNoDurability != null)
                item.BehaviorOnNoDurability = (Item.BehaviorOnNoDurabilityType)this.BehaviorOnNoDurability;

            if (this.CastModifier != null)
                item.CastModifier = (Character.SpellCastModifier)this.CastModifier;

            if (this.CastType != null)
                //At.SetField(item, "m_activateEffectAnimType", (Character.SpellCastType)this.CastType);
                item.m_activateEffectAnimType = (Character.SpellCastType)this.CastType;

            if (this.OverrideSellModifier != null)
                item.m_overrideSellModifier = (float)this.OverrideSellModifier;

            if (this.Tags != null)
            {
                CustomItems.SetItemTags(item, this.Tags, true);
            }

            if (this.StatsHolder != null)
            {
                var desiredType = Serializer.GetGameType(this.StatsHolder.GetType());

                var stats = item.GetComponent<ItemStats>();
                if (!stats)
                {
                    stats = (ItemStats)item.gameObject.AddComponent(desiredType);
                }
                else
                {
                    stats = (ItemStats)UnityHelpers.FixComponentType(desiredType, stats);
                }

                StatsHolder.ApplyToItem(stats);
                item.m_stats = stats;
            }

            ApplyItemVisuals(item);

            SLPackManager.AddLateApplyListener(OnLateApply, item);
        }

        private void OnLateApply(object[] obj)
        {
            var item = obj[0] as Item;

            if (this.ItemExtensions != null)
                SL_ItemExtension.ApplyExtensionList(item, this.ItemExtensions, this.ExtensionsEditBehaviour);

            SL_EffectTransform.ApplyTransformList(item.transform, this.EffectTransforms, this.EffectBehaviour);
        }

        /// <summary>
        /// Applies the ItemVisuals, and checks for pngs and materials to apply in `SLPack\Items\SubfolderPath\Textures\`.
        /// </summary>
        public void ApplyItemVisuals(Item item)
        {
            // Visual Prefabs
            if (this.ItemVisuals != null)
            {
                ItemVisuals.Type = VisualPrefabType.VisualPrefab;
                this.ItemVisuals.ApplyToItem(item);
            }
            else
            {
                CustomItemVisuals.CloneVisualPrefab(item, VisualPrefabType.VisualPrefab);
            }

            if (this.SpecialItemVisuals != null)
            {
                this.SpecialItemVisuals.Type = VisualPrefabType.SpecialVisualPrefabDefault;
                this.SpecialItemVisuals.ApplyToItem(item);
            }
            else
            {
                CustomItemVisuals.CloneVisualPrefab(item, VisualPrefabType.SpecialVisualPrefabDefault);
            }

            if (this.SpecialFemaleItemVisuals != null)
            {
                this.SpecialFemaleItemVisuals.Type = VisualPrefabType.SpecialVisualPrefabFemale;
                this.SpecialFemaleItemVisuals.ApplyToItem(item);
            }
            else
            {
                CustomItemVisuals.CloneVisualPrefab(item, VisualPrefabType.SpecialVisualPrefabFemale);
            }

            // Texture Replacements
            if (!string.IsNullOrEmpty(SLPackName) && SL.GetSLPack(SLPackName) is SLPack && !string.IsNullOrEmpty(this.SubfolderName))
            {
                ReadAndApplyTexturesFolder(item);
            }
        }

        public void ReadAndApplyTexturesFolder(Item item)
        {
            var pack = SL.GetSLPack(this.SLPackName);

            var ctgPath = pack.GetPathForCategory<ItemCategory>();

            var texturesFolder = Path.Combine(ctgPath, this.SubfolderName, "Textures");

            if (!pack.DirectoryExists(texturesFolder))
                return;

            ApplyIconsFromFolder(pack, texturesFolder, item);

            var textures = GetTexturesFromFolder(pack, texturesFolder, out m_serializedMaterials);
            ApplyTexAndMats(textures, m_serializedMaterials, item);
        }

        private void ApplyIconsFromFolder(SLPack pack, string dir, Item item)
        {
            if (pack.FileExists(dir, "icon.png"))
            {
                var tex = pack.LoadTexture2D(dir, "icon.png", false, false);
                var sprite = CustomTextures.CreateSprite(tex, CustomTextures.SpriteBorderTypes.ItemIcon);
                UnityEngine.Object.DontDestroyOnLoad(sprite);
                sprite.name = "icon";
                CustomItemVisuals.SetSpriteLink(item, sprite, false);
            }

            // check for Skill icon
            if (pack.FileExists(dir, "skillicon.png"))
            {
                var tex = pack.LoadTexture2D(dir, "skillicon.png");
                var sprite = CustomTextures.CreateSprite(tex, CustomTextures.SpriteBorderTypes.SkillTreeIcon);
                UnityEngine.Object.DontDestroyOnLoad(sprite);
                sprite.name = "skillicon";
                CustomItemVisuals.SetSpriteLink(item, sprite, true);
            }
        }

        internal static Dictionary<string, List<Texture2D>> GetTexturesFromFolder(SLPack pack, string dir, out Dictionary<string, SL_Material> slMaterials)
        {
            // build dictionary of textures per material
            // Key: Material name (Safe), Value: Texture
            var textures = new Dictionary<string, List<Texture2D>>();

            // also keep a dict of the SL_Material templates
            slMaterials = new Dictionary<string, SL_Material>();

            foreach (var subfolder in pack.GetDirectories(dir))
            {
                var matname = Path.GetFileName(subfolder).ToLower();

                if (slMaterials.ContainsKey(matname))
                    continue;

                SL.Log("Reading material folder " + matname);

                // check for the SL_Material xml
                Dictionary<string, SL_Material.TextureConfig> texCfgDict = null;

                if (pack.FileExists(subfolder, "properties.xml"))
                {
                    var matHolder = pack.ReadXmlDocument<SL_Material>(subfolder, "properties.xml");

                    matHolder.Name = matname;
                    matHolder.m_serializedFolderPath = subfolder;
                    texCfgDict = matHolder.TextureConfigsToDict();
                    slMaterials.Add(matname, matHolder);
                }

                // read the textures
                var texFiles = pack.GetFiles(subfolder, ".png");
                if (texFiles.Length > 0)
                {
                    textures.Add(matname, new List<Texture2D>());

                    foreach (var filepath in texFiles)
                    {
                        var name = Path.GetFileNameWithoutExtension(filepath);

                        var check = name.ToLower();
                        bool linear = check.Contains("normtex") || check == "_bumpmap" || check == "_normalmap";

                        bool mipmap = true;
                        if (texCfgDict != null && texCfgDict.ContainsKey(name))
                        {
                            mipmap = texCfgDict[name].UseMipMap;
                        }

                        // at this point we can safely turn it lower case for compatibility going forward
                        name = name.ToLower();

                        var tex = pack.LoadTexture2D(subfolder, Path.GetFileName(filepath), mipmap, linear);
                        tex.name = name;
                        textures[matname].Add(tex);
                    }
                }
            }

            return textures;
        }

        /// <summary>
        /// Applies textures to the item using the provided dictionary.
        /// </summary>
        /// <param name="textures">Key: Material names (with GetSafeMaterialName), Value: List of Textures to apply, names should match the shader layers of the material.</param>
        /// <param name="slMaterials">[OPTIONAL] Key: Material names with GetSafeMaterialName, Value: SL_Material template to apply.</param>
        /// <param name="item">The item to apply to</param>
        internal static void ApplyTexAndMats(Dictionary<string, List<Texture2D>> textures, Dictionary<string, SL_Material> slMaterials, Item item)
        {
            if (slMaterials == null)
            {
                slMaterials = new Dictionary<string, SL_Material>();
            }

            // apply to mats
            for (int i = 0; i < 3; i++)
            {
                var prefabtype = (VisualPrefabType)i;

                if (!CustomItemVisuals.s_itemVisualLinks.ContainsKey(item.ItemID)
                    || !CustomItemVisuals.s_itemVisualLinks[item.ItemID].GetVisuals(prefabtype))
                {
                    var prefab = CustomItemVisuals.CloneVisualPrefab(item, prefabtype, false);
                    if (!prefab)
                        continue;
                }

                var mats = CustomItemVisuals.GetMaterials(item, prefabtype);

                if (mats == null || mats.Length < 1)
                    continue;

                foreach (var mat in mats)
                {
                    var matname = CustomItemVisuals.GetSafeMaterialName(mat.name).ToLower();

                    // apply the SL_material template first (set shader, etc)
                    SL_Material matHolder = null;
                    if (slMaterials.ContainsKey(matname))
                    {
                        matHolder = slMaterials[matname];
                        matHolder.ApplyToMaterial(mat);
                    }
                    else if (!textures.ContainsKey(matname))
                        continue;

                    // build list of actual shader layer names.
                    // Key: ToLower(), Value: original.
                    Dictionary<string, string> layersToLower = new Dictionary<string, string>();
                    foreach (var layer in mat.GetTexturePropertyNames())
                        layersToLower.Add(layer.ToLower(), layer);

                    // set actual textures
                    foreach (var tex in textures[matname])
                    {
                        try
                        {
                            if (mat.HasProperty(tex.name))
                            {
                                mat.SetTexture(tex.name, tex);
                                //SL.Log("Set texture " + tex.name + " on " + matname);
                            }
                            else if (layersToLower.ContainsKey(tex.name))
                            {
                                var realname = layersToLower[tex.name];
                                mat.SetTexture(realname, tex);
                                //SL.Log("Set texture " + realname + " on " + matname);
                            }
                            else
                                SL.Log("Couldn't find a shader property called " + tex.name + "!");
                        }
                        catch
                        {
                            SL.Log("Exception setting texture " + tex.name + " on material!");
                        }
                    }

                    // finalize texture settings after they've been applied
                    if (matHolder != null)
                    {
                        matHolder.ApplyTextureSettings(mat);
                    }
                }
            }
        }

        // ***********************  FOR SERIALIZING AN ITEM INTO A TEMPLATE  *********************** //

        public static SL_Item ParseItemToTemplate(Item item)
        {
            SL.Log("Parsing item to template: " + item.Name);

            var type = Serializer.GetBestSLType(item.GetType());

            if (type == null)
            {
                SL.LogWarning("Could not get SL_Type for " + item.GetType().FullName + "!");
                return null;
            }

            var holder = (SL_Item)Activator.CreateInstance(type);

            holder.SerializeItem(item);

            return holder;
        }

        public virtual void SerializeItem(Item item)
        {
            Name = item.Name;
            Description = item.Description;
            Target_ItemID = item.ItemID;
            LegacyItemID = item.LegacyItemID;
            CastLocomotionEnabled = item.CastLocomotionEnabled;
            CastModifier = item.CastModifier;
            CastSheatheRequired = item.CastSheathRequired;
            GroupItemInDisplay = item.GroupItemInDisplay;
            HasPhysicsWhenWorld = item.HasPhysicsWhenWorld;
            IsPickable = item.IsPickable;
            IsUsable = item.IsUsable;
            QtyRemovedOnUse = item.QtyRemovedOnUse;
            MobileCastMovementMult = item.MobileCastMovementMult;
            RepairedInRest = item.RepairedInRest;
            BehaviorOnNoDurability = item.BehaviorOnNoDurability;
            CastType = item.m_activateEffectAnimType;
            this.OverrideSellModifier = item.m_overrideSellModifier;

            if (item.GetComponent<ItemStats>() is ItemStats stats)
            {
                StatsHolder = SL_ItemStats.ParseItemStats(stats);
            }

            var extensions = item.gameObject.GetComponentsInChildren<ItemExtension>();
            var extList = new List<SL_ItemExtension>();
            if (extensions != null)
            {
                foreach (var ext in extensions)
                {
                    var extHolder = SL_ItemExtension.SerializeExtension(ext);
                    extList.Add(extHolder);
                }
            }
            ItemExtensions = extList.ToArray();

            var tags = new List<string>();
            if (item.Tags != null)
            {
                foreach (Tag tag in item.Tags)
                {
                    tags.Add(tag.TagName);
                }
            }
            Tags = tags.ToArray();

            if (item.transform.childCount > 0)
            {
                var children = new List<SL_EffectTransform>();
                foreach (Transform child in item.transform)
                {
                    var effectsChild = SL_EffectTransform.ParseTransform(child);

                    if (effectsChild.HasContent)
                    {
                        children.Add(effectsChild);
                    }
                }
                EffectTransforms = children.ToArray();
            }

            if (item.HasVisualPrefab && ResourcesPrefabManager.Instance.GetItemVisualPrefab(item.VisualPrefabPath).GetComponent<ItemVisual>() is ItemVisual visual)
            {
                ItemVisuals = SL_ItemVisual.ParseVisualToTemplate(item, VisualPrefabType.VisualPrefab, visual);
                ItemVisuals.Type = VisualPrefabType.VisualPrefab;
            }
            if (item.HasSpecialVisualDefaultPrefab)
            {
                SpecialItemVisuals = SL_ItemVisual.ParseVisualToTemplate(item, VisualPrefabType.SpecialVisualPrefabDefault, ResourcesPrefabManager.Instance.GetItemVisualPrefab(item.SpecialVisualPrefabDefaultPath).GetComponent<ItemVisual>());
                SpecialItemVisuals.Type = VisualPrefabType.SpecialVisualPrefabDefault;
            }
            if (item.HasSpecialVisualFemalePrefab)
            {
                SpecialFemaleItemVisuals = SL_ItemVisual.ParseVisualToTemplate(item, VisualPrefabType.SpecialVisualPrefabFemale, ResourcesPrefabManager.Instance.GetItemVisualPrefab(item.SpecialVisualPrefabFemalePath).GetComponent<ItemVisual>());
                SpecialFemaleItemVisuals.Type = VisualPrefabType.SpecialVisualPrefabFemale;
            }
        }

        public static void SaveItemIcons(Item item, string dir)
        {
            SL.Log("Saving item icons...");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            try
            {
                Sprite icon = item.m_itemIcon;
                if (!icon)
                    icon = ResourcesPrefabManager.Instance.GetItemIcon(item);
                if (!icon)
                    icon = item.DefaultIcon;

                CustomTextures.SaveIconAsPNG(icon, dir, "icon");

            }
            catch (Exception e)
            {
                SL.Log(e.ToString());
            }

            if (item is Skill skill && skill.SkillTreeIcon)
                CustomTextures.SaveIconAsPNG(skill.SkillTreeIcon, dir, "skillicon");

            if (item is LevelAttackSkill levelAtkSkill)
            {
                var stages = levelAtkSkill.m_skillStages;
                int idx = 1;
                foreach (var stage in stages)
                {
                    idx++;
                    if (stage.StageIcon)
                        CustomTextures.SaveIconAsPNG(stage.StageIcon, dir, $"icon{idx}");
                }
            }
        }

        /// <summary>
        /// Saves textures from an Item to a directory.
        /// </summary>
        /// <param name="item">The item to apply to.</param>
        /// <param name="dir">Full path, relative to Outward folder</param>
        /// <param name="materials">The SL_Material templates which were generated.</param>
        public static void SaveItemTextures(Item item, string dir, out Dictionary<string, SL_Material> materials)
        {
            SL.Log("Saving item textures...");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            materials = new Dictionary<string, SL_Material>();

            for (int i = 0; i < 3; i++)
            {
                //SL.Log("Checking materials (" + ((VisualPrefabType)i) + ")");

                if (CustomItemVisuals.GetMaterials(item, (VisualPrefabType)i) is Material[] mats)
                {
                    foreach (var mat in mats)
                    {
                        var name = CustomItemVisuals.GetSafeMaterialName(mat.name);

                        if (materials.ContainsKey(name))
                            continue;

                        string subdir = Path.Combine(dir, name);

                        var matHolder = SL_Material.ParseMaterial(mat);
                        Serializer.SaveToXml(subdir, "properties", matHolder);

                        materials.Add(name, matHolder);

                        SaveMaterialTextures(mat, subdir);
                    }
                }
            }
        }

        private static void SaveMaterialTextures(Material mat, string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            foreach (var texName in mat.GetTexturePropertyNames())
            {
                if (mat.GetTexture(texName) is Texture tex)
                {
                    bool normal = texName.Contains("NormTex") || texName.Contains("BumpMap") || texName == "_NormalMap";

                    CustomTextures.SaveTextureAsPNG(tex as Texture2D, dir, texName, normal);
                }
            }
        }

        // Legacy

        [Obsolete("Use SL_Item.Apply() instead (renamed).")]
        public void ApplyTemplateToItem() => Apply();

        [Obsolete("Use 'AddOnInstanceStartListener' instead (renamed)")]
        public void OnInstanceStart(Action<Item> callback) => AddOnInstanceStartListener(callback);
    }
}
