using System;
using System.Xml.Serialization;
using UnityEngine;

namespace SideLoader
{
    [SL_Serialized]
    public class SL_ItemVisual
    {
        /// <summary>You don't need to set this if you're setting a value in an SL_Item template, SideLoader will figure this out for you.</summary>
        [XmlIgnore]
        public VisualPrefabType Type;

        /// <summary>
        /// Used to set the visual prefab from an existing prefab at the given path. Checks the Unity Resources "folder" for the asset path.
        /// </summary>
        public string ResourcesPrefabPath = "";

        /// <summary>SLPack using for AssetBundle visuals</summary>
        public string Prefab_SLPack = "";
        /// <summary>AssetBundle file name inside specified Prefab_SLPack</summary>
        public string Prefab_AssetBundle = "";
        /// <summary>Prefab GameObject name inside specified Prefab_AssetBundle</summary>
        public string Prefab_Name = "";

        /// <summary>Optional, directly set the position</summary>
        public Vector3? Position;
        /// <summary>Optional, directly set the rotation</summary>
        public Vector3? Rotation;
        /// <summary>Optional, directly set the scale</summary>
        public Vector3? Scale;

        /// <summary>Optional, add offset to position</summary>
        public Vector3? PositionOffset;
        /// <summary>Optional, add offset to rotation</summary>
        public Vector3? RotationOffset;

        /// <summary>
        /// Apply the SL_ItemVisual prefab to the Item.
        /// </summary>
        /// <param name="item">The Item to set to.</param>
        public void ApplyToItem(Item item)
        {
            if (CustomItemVisuals.GetOrigItemVisuals(item, Type) is Transform origPrefab)
            {
                bool setPrefab = false;

                // Check for AssetBundle Prefabs first
                if (!string.IsNullOrEmpty(Prefab_SLPack) && SL.GetSLPack(Prefab_SLPack) is SLPack pack)
                {
                    if (pack.AssetBundles.ContainsKey(Prefab_AssetBundle))
                    {
                        var newVisuals = pack.AssetBundles[Prefab_AssetBundle].LoadAsset<GameObject>(Prefab_Name);

                        origPrefab = SetCustomVisualPrefab(item, this.Type, newVisuals.transform, origPrefab).transform;

                        SL.Log("Loaded custom visual prefab: " + newVisuals.name);
                        setPrefab = true;
                    }
                    else
                        SL.LogWarning($"Cannot find asset bundle by name of '{Prefab_AssetBundle}'!");
                }

                // if not set, check for ResourcesPrefabPath.
                if (!setPrefab && !string.IsNullOrEmpty(ResourcesPrefabPath))
                {
                    // Only set this if the user has defined a different value than what exists on the item.
                    bool set = false;
                    switch (Type)
                    {
                        case VisualPrefabType.VisualPrefab:
                            set = item.VisualPrefabPath == ResourcesPrefabPath; break;
                        case VisualPrefabType.SpecialVisualPrefabDefault:
                            set = item.SpecialVisualPrefabDefaultPath == ResourcesPrefabPath; break;
                        case VisualPrefabType.SpecialVisualPrefabFemale:
                            set = item.SpecialVisualPrefabFemalePath == ResourcesPrefabPath; break;
                    }

                    if (!set)
                    {
                        // Get the original visual prefab to clone from
                        var orig = ResourcesPrefabManager.Instance.GetItemVisualPrefab(ResourcesPrefabPath);

                        if (!orig)
                            SL.Log("SL_ItemVisual: Could not find an Item Visual at the Resources path: " + ResourcesPrefabPath);
                        else
                        {
                            CustomItemVisuals.CloneAndSetVisuals(item, orig.gameObject, Type);

                            switch (Type)
                            {
                                case VisualPrefabType.VisualPrefab:
                                    item.m_visualPrefabPath = ResourcesPrefabPath;
                                    break;
                                case VisualPrefabType.SpecialVisualPrefabDefault:
                                    item.m_specialVisualPrefabDefaultPath = ResourcesPrefabPath;
                                    break;
                                case VisualPrefabType.SpecialVisualPrefabFemale:
                                    item.m_specialVisualPrefabFemalePath = ResourcesPrefabPath;
                                    break;
                            }

                            setPrefab = true;
                        }
                    }
                }

                // If we didn't change the Visual Prefab in any way, clone the original to avoid conflicts.
                if (!setPrefab)
                    origPrefab = CustomItemVisuals.CloneVisualPrefab(item, Type).transform;

                // Get the actual visuals (for weapons and a lot of items, this is not the base prefab).
                Transform actualVisuals = origPrefab.transform;
                if (this.Type == VisualPrefabType.VisualPrefab)
                {
                    if (origPrefab.childCount > 0)
                    {
                        foreach (Transform child in origPrefab)
                        {
                            if (child.gameObject.activeSelf 
                                && (child.GetComponent<BoxCollider>() || child.GetComponent<MeshCollider>()) 
                                && child.GetComponent<MeshRenderer>())
                            {
                                actualVisuals = child;
                                break;
                            }
                        }
                    }
                }

                if (actualVisuals)
                {
                    var visualComp = origPrefab.GetComponent<ItemVisual>();
                    ApplyItemVisualSettings(visualComp, actualVisuals);
                }
                else
                    SL.LogWarning($"Could not find the visual model for '{origPrefab.name}'! " +
                        $"Ensure the model has a BoxCollider and MeshRenderer on the same GameObject!");
            }
        }

        internal GameObject SetCustomVisualPrefab(Item item, VisualPrefabType type, Transform newVisuals, Transform oldVisuals)
        {
            SL.Log($"Setting the {type} for {item.Name}");

            var basePrefab = GameObject.Instantiate(oldVisuals.gameObject);
            GameObject.DontDestroyOnLoad(basePrefab);
            basePrefab.SetActive(false);

            var visualModel = UnityEngine.Object.Instantiate(newVisuals.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(visualModel.gameObject);

            if (type == VisualPrefabType.VisualPrefab)
            {
                // At the moment, the only thing we replace on ItemVisuals is the 3d model, everything else is a clone.
                foreach (Transform child in basePrefab.transform)
                {
                    // the real 3d model will always have boxcollider and meshrenderer. this is the object we want to replace.
                    if (child.GetComponent<BoxCollider>() && child.GetComponent<MeshRenderer>())
                    {
                        child.gameObject.SetActive(false);

                        visualModel.transform.position = child.position;
                        visualModel.transform.rotation = child.rotation;

                        visualModel.transform.parent = child.parent;

                        break;
                    }
                }

                basePrefab.name = visualModel.name;
                CustomItemVisuals.SetVisualPrefabLink(item, basePrefab, type);

                SL.Log("Setup visual prefab link: " + item.Name + " -> " + basePrefab.name);

                return basePrefab;
            }
            else
            {
                if (!visualModel.GetComponent<ItemVisual>() && basePrefab.GetComponent<ItemVisual>() is ItemVisual itemVisual)
                {
                    if (itemVisual is ArmorVisuals)
                    {
                        var armorV = visualModel.AddComponent<ArmorVisuals>();
                        armorV.ArmorExtras = new ArmorVisualExtra[0];
                    }
                    else
                        visualModel.AddComponent<ItemVisual>();
                }

                visualModel.transform.position = basePrefab.transform.position;
                visualModel.transform.rotation = basePrefab.transform.rotation;
                visualModel.gameObject.SetActive(false);

                // we no longer need the clone for these visuals. we should clean it up.
                UnityEngine.Object.DestroyImmediate(basePrefab.gameObject);

                CustomItemVisuals.SetVisualPrefabLink(item, visualModel, type);
                SL.Log("Setup visual prefab link: " + item.Name + " -> " + visualModel.name);

                return visualModel;
            }
        }

        internal virtual void ApplyItemVisualSettings(ItemVisual itemVisual, Transform visuals)
        {
            SL.Log($"Applying ItemVisuals settings to " + visuals.name);

            if (Position != null)
                visuals.localPosition = (Vector3)Position;
            if (Rotation != null)
                visuals.eulerAngles = (Vector3)Rotation;
            if (Scale != null)
                visuals.localScale = (Vector3)Scale;
            if (PositionOffset != null)
                visuals.localPosition += (Vector3)PositionOffset;
            if (RotationOffset != null)
                visuals.eulerAngles += (Vector3)RotationOffset;
        }

        public static SL_ItemVisual ParseVisualToTemplate(Item item, VisualPrefabType type, ItemVisual itemVisual)
        {
            var template = (SL_ItemVisual)Activator.CreateInstance(Serializer.GetBestSLType(itemVisual.GetType()));
            template.SerializeItemVisuals(itemVisual);
            switch (type)
            {
                case VisualPrefabType.VisualPrefab:
                    template.ResourcesPrefabPath = item.VisualPrefabPath; break;
                case VisualPrefabType.SpecialVisualPrefabDefault:
                    template.ResourcesPrefabPath = item.SpecialVisualPrefabDefaultPath; break;
                case VisualPrefabType.SpecialVisualPrefabFemale:
                    template.ResourcesPrefabPath = item.SpecialVisualPrefabFemalePath; break;
            };
            return template;
        }

        public virtual void SerializeItemVisuals(ItemVisual itemVisual)
        {
            if (!itemVisual.gameObject.GetComponentInChildren<SkinnedMeshRenderer>())
            {
                var t = itemVisual.transform;

                foreach (Transform child in t)
                {
                    if (child.GetComponent<MeshRenderer>() && child.GetComponent<BoxCollider>())
                    {
                        Position = child.localPosition;
                        Rotation = child.eulerAngles;
                        Scale = child.localScale;

                        break;
                    }
                }
            }
            else
            {
                Position = itemVisual.transform.position;
                Rotation = itemVisual.transform.rotation.eulerAngles;
                Scale = itemVisual.transform.localScale;
            }
        }
    }

    /// <summary>
    /// Helper enum for the possible Visual Prefab types on Items. 
    /// </summary>
    public enum VisualPrefabType
    {
        /// <summary>Item.VisualPrefab</summary>
        VisualPrefab,
        /// <summary>Item.SpecialVisualPrefabDefault</summary>
        SpecialVisualPrefabDefault,
        /// <summary>Item.SpecialVisualPrefabFemale</summary>
        SpecialVisualPrefabFemale
    }
}
