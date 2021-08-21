using Localizer;
using SideLoader.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SideLoader
{
    /// <summary>
    /// SideLoader's helper class for Custom Items.
    /// </summary>
    public class CustomItems
    {
        private static readonly Dictionary<int, Item> OrigItemPrefabs = new Dictionary<int, Item>();

        /// <summary> Will return the true original prefab for this Item ID. </summary>
        public static Item GetOriginalItemPrefab(int ItemID)
        {
            if (OrigItemPrefabs.ContainsKey(ItemID))
            {
                return OrigItemPrefabs[ItemID];
            }
            else
            {
                return ResourcesPrefabManager.Instance.GetItemPrefab(ItemID);
            }
        }

        /// <summary>
        /// Simple method to apply a SL_Item template. 
        /// If defining a custom item after SL.OnPacksLoaded it will be applied instantly, otherwise it uses a callback to be applied later.
        /// </summary>
        /// <param name="template"></param>
        /// <returns>Your new custom item (or the original item, if modifying an existing one)</returns>
        public static Item CreateCustomItem(SL_Item template)
        {
            return CreateCustomItem(template.Target_ItemID, template.New_ItemID, template.Name, template);
        }

        /// <summary>
        /// Clones an item prefab and returns the clone to you. Caches the original prefab for other mods or other custom items to reference.
        /// </summary>
        /// <param name="cloneTargetID">The Item ID of the Item you want to clone from</param>
        /// <param name="newID">The new Item ID for your cloned item. Can be the same as the target, will overwrite.</param>
        /// <param name="name">Only used for the gameObject name, not the actual Item Name. This is the name thats used in Debug Menus.</param>
        /// <param name="template">[Optional] If provided, the item component may be changed to match the type of the template if necessary.</param>
        /// <returns>Your cloned Item prefab</returns>
        public static Item CreateCustomItem(int cloneTargetID, int newID, string name, SL_Item template = null)
        {
            Item original = GetOriginalItemPrefab(cloneTargetID);

            if (!original)
            {
                SL.LogError("CustomItems.CreateCustomItem - Error! Could not find the clone target Item ID: " + cloneTargetID);
                return null;
            }

            Item item;

            original.transform.ResetLocal();

            // modifying an existing item
            if (newID == cloneTargetID)
            {
                // Modifying the original prefab for the first time. Cache it in case someone else wants the true original.
                if (!OrigItemPrefabs.ContainsKey(newID))
                {
                    var cached = GameObject.Instantiate(original.gameObject).GetComponent<Item>();
                    cached.gameObject.SetActive(false);
                    GameObject.DontDestroyOnLoad(cached.gameObject);
                    cached.transform.parent = SL.CloneHolder;
                    OrigItemPrefabs.Add(cached.ItemID, cached);
                }

                // apply to the original item prefab. this ensures direct prefab references to this item reflect the changes.
                item = original;
            }
            else // making a new item
            {
                item = GameObject.Instantiate(original.gameObject).GetComponent<Item>();
                item.gameObject.SetActive(false);
                item.gameObject.name = newID + "_" + name;

                // fix for name and description localization
                SetNameAndDescription(item, original.Name, original.Description);
            }

            if (template != null)
            {
                var gameType = Serializer.GetGameType(template.GetType());
                if (gameType != item.GetType())
                {
                    item = UnityHelpers.FixComponentType(gameType, item) as Item;
                }
            }

            item.ItemID = newID;
            SetItemID(newID, item);

            // Do this so that any changes we make are not destroyed on scene changes.
            // This is needed whether this is a clone or a new item.
            GameObject.DontDestroyOnLoad(item.gameObject);

            item.transform.parent = SL.CloneHolder;
            item.gameObject.SetActive(true);

            return item;
        }

        /// <summary>
        /// Sets the ResourcesPrefabManager.ITEM_PREFABS dictionary for a custom Item ID. Will overwrite if the ID exists.
        /// This is called by CustomItems.CreateCustomItem
        /// </summary>
        /// <param name="_ID">The Item ID you want to set</param>
        /// <param name="item">The Item prefab</param>
        public static void SetItemID(int _ID, Item item)
        {
            //SL.Log("Setting a custom Item ID to the ResourcesPrefabManager dictionary. ID: " + ID + ", item name: " + item.Name);

            var id = _ID.ToString();
            if (References.RPM_ITEM_PREFABS.ContainsKey(id))
            {
                References.RPM_ITEM_PREFABS[id] = item;
            }
            else
            {
                References.RPM_ITEM_PREFABS.Add(id, item);
            }
        }

        /// <summary> Helper for setting an Item's name easily </summary>
        public static void SetName(Item item, string name)
        {
            SetNameAndDescription(item, name, item.Description);
        }

        /// <summary> Helper for setting an Item's description easily </summary>
        public static void SetDescription(Item item, string description)
        {
            SetNameAndDescription(item, item.Name, description);
        }

        internal static readonly Dictionary<int, ItemLocalization> s_customLocalizations = new Dictionary<int, ItemLocalization>();

        /// <summary> Set both name and description. Used by SetName and SetDescription. </summary>
        public static void SetNameAndDescription(Item item, string _name, string _description)
        {
            var name = _name ?? "";
            var desc = _description ?? "";

            // set the local fields on the item (this should be enough for 99% of cases)

            item.m_name = name;
            item.m_localizedName = name;
            item.m_lastNameLang = LocalizationManager.Instance.CurrentLanguage;
            item.m_localizedDescription = desc;
            item.m_lastDescLang = LocalizationManager.Instance.CurrentLanguage;

            // set the localization to the LocalizationManager dictionary

            var loc = new ItemLocalization(name, desc);

            if (References.ITEM_LOCALIZATION.ContainsKey(item.ItemID))
                References.ITEM_LOCALIZATION[item.ItemID] = loc;
            else
                References.ITEM_LOCALIZATION.Add(item.ItemID, loc);

            // keep track of the custom localization in case the user changes language

            if (s_customLocalizations.ContainsKey(item.ItemID))
                s_customLocalizations[item.ItemID] = loc;
            else
                s_customLocalizations.Add(item.ItemID, loc);

            // If the item is in the "duplicate" localization dicts, remove it.

            if (References.LOCALIZATION_DUPLICATE_NAME.ContainsKey(item.ItemID))
                References.LOCALIZATION_DUPLICATE_NAME.Remove(item.ItemID);

            if (References.LOCALIZATION_DUPLICATE_DESC.ContainsKey(item.ItemID))
                References.LOCALIZATION_DUPLICATE_DESC.Remove(item.ItemID);
        }

        // ================ TAGS ================ //

        /// <summary>
        /// Gets a tag from a string tag name. Note: This just calls CustomTags.GetTag(tagName, logging).
        /// </summary>
        public static Tag GetTag(string tagName, bool logging = true)
        {
            return CustomTags.GetTag(tagName, logging);
        }

        /// <summary>
        /// Creates a new custom tag. Note: This just calls CustomTags.CreateTag(tagName).
        /// </summary>
        public static void CreateTag(string tagName)
        {
            CustomTags.CreateTag(tagName);
        }

        public static void SetItemTags(Item item, string[] tags, bool destroyExisting)
        {
            var tagsource = CustomTags.SetTagSource(item.gameObject, tags, destroyExisting);
            item.m_tagSource = (TagSource)tagsource;
        }

        [Obsolete("Use SL.DestroyChildren instead! (Moved)")]
        public static void DestroyChildren(Transform t, bool destroyContent = false)
        {
            UnityHelpers.DestroyChildren(t, destroyContent);
        }
    }
}
