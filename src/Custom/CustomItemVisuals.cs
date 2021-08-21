using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SideLoader
{
    /// <summary>SideLoader's helper class for Custom Item Visuls.</summary>
    public class CustomItemVisuals
    {
        /// <summary>
        /// Used internally for managing custom item visuals for the ResourcesPrefabManager.
        /// </summary>
        public class ItemVisualsLink
        {
            /// <summary>
            /// Returns the linked ItemVisuals for the provided VisualPrefabType (if any), otherwise null.
            /// </summary>
            /// <param name="type">The type of Visual Prefab you want.</param>
            /// <returns>The linked Transform, or null.</returns>
            public Transform GetVisuals(VisualPrefabType type)
            {
                switch (type)
                {
                    case VisualPrefabType.VisualPrefab: return ItemVisuals;
                    case VisualPrefabType.SpecialVisualPrefabDefault: return ItemSpecialVisuals;
                    case VisualPrefabType.SpecialVisualPrefabFemale: return ItemSpecialFemaleVisuals;
                    default: return null;
                }
            }

            public Sprite ItemIcon;
            public Sprite SkillTreeIcon;

            public Transform ItemVisuals;
            public Transform ItemSpecialVisuals;
            public Transform ItemSpecialFemaleVisuals;
        }

        internal static readonly Dictionary<int, ItemVisualsLink> s_itemVisualLinks = new Dictionary<int, ItemVisualsLink>();

        // Regex: Match anything up to " (" 
        private static readonly Regex materialRegex = new Regex(@".+?(?= \()");

        #region Main public API

        /// <summary>
        /// Returns the true name of the given material name (removes "(Clone)" and "(Instance)", etc)
        /// </summary>
        public static string GetSafeMaterialName(string origName) => materialRegex.Match(origName).Value;

        public static ItemVisualsLink GetItemVisualLink(Item item)
        {
            if (s_itemVisualLinks.ContainsKey(item.ItemID))
                return s_itemVisualLinks[item.ItemID];

            return null;
        }

        public static ItemVisualsLink GetOrAddVisualLink(Item item)
        {
            if (!s_itemVisualLinks.ContainsKey(item.ItemID))
                s_itemVisualLinks.Add(item.ItemID, new ItemVisualsLink());

            var link = s_itemVisualLinks[item.ItemID];

            return link;
        }

        public static void SetVisualPrefabLink(Item item, GameObject newVisuals, VisualPrefabType type)
        {
            var link = GetOrAddVisualLink(item);
            switch (type)
            {
                case VisualPrefabType.VisualPrefab:
                    link.ItemVisuals = newVisuals.transform;
                    break;
                case VisualPrefabType.SpecialVisualPrefabDefault:
                    link.ItemSpecialVisuals = newVisuals.transform;
                    break;
                case VisualPrefabType.SpecialVisualPrefabFemale:
                    link.ItemSpecialFemaleVisuals = newVisuals.transform;
                    break;
            }
        }

        /// <summary>
        /// Helper to set the ItemVisualLink Icon or SkillTreeIcon for an Item.
        /// </summary>
        /// <param name="item">The item you want to set to.</param>
        /// <param name="sprite">The Sprite you want to set.</param>
        /// <param name="skill">Whether this is a "small skill tree icon", or just the main item icon.</param>
        public static void SetSpriteLink(Item item, Sprite sprite, bool skill = false)
        {
            var link = GetOrAddVisualLink(item);

            if (skill)
            {
                (item as Skill).SkillTreeIcon = sprite;
                link.SkillTreeIcon = sprite;
            }
            else
            {
                item.m_itemIcon = sprite;

                if (item.HasDefaultIcon)
                    item.m_itemIconPath = "notnull";

                link.ItemIcon = sprite;
            }
        }

        /// <summary>Returns the original Item Visuals for the given Item and VisualPrefabType</summary>
        public static Transform GetOrigItemVisuals(Item item, VisualPrefabType type)
        {
            Transform prefab = null;
            switch (type)
            {
                case VisualPrefabType.VisualPrefab:
                    prefab = ResourcesPrefabManager.Instance.GetItemVisualPrefab(item.VisualPrefabPath);
                    break;
                case VisualPrefabType.SpecialVisualPrefabDefault:
                    prefab = ResourcesPrefabManager.Instance.GetItemVisualPrefab(item.SpecialVisualPrefabDefaultPath);
                    break;
                case VisualPrefabType.SpecialVisualPrefabFemale:
                    prefab = ResourcesPrefabManager.Instance.GetItemVisualPrefab(item.SpecialVisualPrefabFemalePath);
                    break;
            }
            return prefab;
        }

        /// <summary> Clone's an items current visual prefab (and materials), then sets this item's visuals to the new cloned prefab. </summary>
        public static GameObject CloneVisualPrefab(Item item, VisualPrefabType type, bool logging = false)
        {
            var prefab = GetOrigItemVisuals(item, type);

            if (!prefab)
            {
                if (logging)
                    SL.Log("Error, no VisualPrefabType defined or could not find visual prefab of that type!");
                return null;
            }

            return CloneAndSetVisuals(item, prefab.gameObject, type);
        }

        /// <summary>
        /// Clones the provided 'prefab' GameObject, and sets it to the provided Item and VisualPrefabType.
        /// </summary>
        /// <param name="item">The Item to apply to.</param>
        /// <param name="newPrefab">The visual prefab to clone and set.</param>
        /// <param name="type">The Type of VisualPrefab you are setting.</param>
        /// <returns>The cloned gameobject.</returns>
        public static GameObject CloneAndSetVisuals(Item item, GameObject newPrefab, VisualPrefabType type)
        {
            // Clone the visual prefab 
            var newVisuals = UnityEngine.Object.Instantiate(newPrefab.gameObject);
            newVisuals.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(newVisuals);

            // add to our CustomVisualPrefab dictionary
            SetVisualPrefabLink(item, newVisuals, type);

            // Clone the materials too so that changes to them don't affect the original item visuals
            foreach (var skinnedMesh in newVisuals.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mats = skinnedMesh.materials;

                for (int i = 0; i < mats.Length; i++)
                {
                    var newmat = UnityEngine.Object.Instantiate(mats[i]);
                    UnityEngine.Object.DontDestroyOnLoad(newmat);
                    mats[i] = newmat;
                }

                skinnedMesh.materials = mats;
            }

            foreach (var mesh in newVisuals.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = mesh.materials;

                for (int i = 0; i < mats.Length; i++)
                {
                    var newmat = UnityEngine.Object.Instantiate(mats[i]);
                    UnityEngine.Object.DontDestroyOnLoad(newmat);
                    mats[i] = newmat;
                }

                mesh.materials = mats;
            }

            newVisuals.transform.parent = SL.CloneHolder;

            return newVisuals;
        }

        /// <summary>
        /// Gets an array of the Materials on the given visual prefab type for the given item.
        /// These are actual references to the Materials, not a copy like Unity's Renderer.Materials[]
        /// </summary>
        public static Material[] GetMaterials(Item item, VisualPrefabType type)
        {
            Transform prefab = null;
            if (s_itemVisualLinks.ContainsKey(item.ItemID))
            {
                var link = s_itemVisualLinks[item.ItemID];
                switch (type)
                {
                    case VisualPrefabType.VisualPrefab:
                        prefab = link.ItemVisuals; break;
                    case VisualPrefabType.SpecialVisualPrefabDefault:
                        prefab = link.ItemSpecialVisuals; break;
                    case VisualPrefabType.SpecialVisualPrefabFemale:
                        prefab = link.ItemSpecialFemaleVisuals; break;
                }
            }

            if (!prefab)
                prefab = GetOrigItemVisuals(item, type);

            if (prefab)
            {
                var mats = new List<Material>();

                foreach (var skinnedMesh in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())
                    mats.AddRange(skinnedMesh.materials);

                foreach (var mesh in prefab.GetComponentsInChildren<MeshRenderer>())
                    mats.AddRange(mesh.materials);

                return mats.ToArray();
            }

            //SL.Log("No material found for this prefab/item!");
            return null;
        }

        #endregion

        #region AssetBundle item textures

        /// <summary>
        /// Searches the provided AssetBundle for folders in the expected format, and applies textures to the corresponding Item.
        /// Each item must have its own sub-folder, where the name of this folder starts with the Item's ItemID.
        /// The folder name can have anything else after the ID, but it must start with the ID.
        /// Eg., '2000010_IronSword\' would be valid to set the textures on the Iron Sword. 
        /// The textures should be placed inside this folder and should match the Shader Layer names of the texture (the same way you set Item textures from a folder).
        /// </summary>
        /// <param name="bundle">The AssetBundle to apply Textures from.</param>
        public static void ApplyTexturesFromAssetBundle(AssetBundle bundle)
        {
            // Keys: Item IDs
            // Values: List of Textures/Sprites to apply to the item
            // The ItemTextures Value Key is the Material name, and the List<Texture2D> are the actual textures.
            var itemTextures = new Dictionary<int, Dictionary<string, List<Texture2D>>>();
            var icons = new Dictionary<int, List<Sprite>>();

            string[] names = bundle.GetAllAssetNames();
            foreach (var name in names)
            {
                try
                {
                    SL.Log("Loading texture from AssetBundle, path: " + name);

                    Texture2D tex = bundle.LoadAsset<Texture2D>(name);

                    // cleanup the name (remove ".png")
                    tex.name = tex.name.Replace(".png", "");

                    // Split the assetbundle path by forward slashes
                    string[] splitPath = name.Split('/');

                    // Get the ID from the first 7 characters of the path
                    int id = int.Parse(splitPath[1].Substring(0, 7));

                    // Identify icons by name
                    if (tex.name.Contains("icon"))
                    {
                        if (!icons.ContainsKey(id))
                            icons.Add(id, new List<Sprite>());

                        Sprite sprite;
                        if (tex.name.Contains("skill"))
                            sprite = CustomTextures.CreateSprite(tex, CustomTextures.SpriteBorderTypes.SkillTreeIcon);
                        else
                            sprite = CustomTextures.CreateSprite(tex, CustomTextures.SpriteBorderTypes.ItemIcon);

                        icons[id].Add(sprite);
                    }
                    else // is not an icon
                    {
                        var mat = "";
                        if (splitPath[2] == "textures")
                            mat = splitPath[3];
                        else
                            mat = splitPath[2];

                        if (!itemTextures.ContainsKey(id))
                            itemTextures.Add(id, new Dictionary<string, List<Texture2D>>());

                        if (!itemTextures[id].ContainsKey(mat))
                            itemTextures[id].Add(mat, new List<Texture2D>());

                        itemTextures[id][mat].Add(tex);
                    }
                }
                catch (InvalidCastException)
                {
                    // suppress
                }
                catch (Exception ex)
                {
                    SL.Log("Exception loading textures from asset bundle!");
                    SL.Log(ex.Message);
                    SL.Log(ex.StackTrace);
                }
            }

            // Apply material textures
            foreach (var entry in itemTextures)
            {
                if (ResourcesPrefabManager.Instance.GetItemPrefab(entry.Key) is Item item)
                    SL_Item.ApplyTexAndMats(entry.Value, null, item);
            }

            // Apply icons
            foreach (var entry in icons)
            {
                if (ResourcesPrefabManager.Instance.GetItemPrefab(entry.Key) is Item item)
                    ApplyIconsByName(entry.Value.ToArray(), item);
            }
        }

        public static void ApplyIconsByName(Sprite[] icons, Item item)
        {
            foreach (var sprite in icons)
            {
                if (sprite.name.ToLower() == "icon")
                    SetSpriteLink(item, sprite, false);
                else if (sprite.name.ToLower() == "skillicon")
                    SetSpriteLink(item, sprite, true);
            }
        }

        #endregion
    }
}
