using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SideLoader
{
    /// <summary>
    /// A helper class for getting Tags more easily, and for creating new ones too.
    /// </summary>
    public class CustomTags
    {
        /// <summary>
        /// Returns the game's actual Tag for the string you provide, if it exists.
        /// </summary>
        /// <param name="TagName">Eg "Food", "Blade", etc...</param>
        /// <param name="logging">Whether to log error messages to debug console or not (if tag doesnt exist)</param>
        /// <returns></returns>
        public static Tag GetTag(string TagName, bool logging = true)
        {
            var tags = TagSourceManager.Instance.m_tags;

            var tag = tags.FirstOrDefault(x => x.TagName == TagName);

            if (tag.TagName == TagName)
                return tag;
            else
            {
                if (logging)
                {
                    SL.Log("GetTag - Could not find a tag by the name: " + TagName);
                }
                return Tag.None;
            }
        }

        /// <summary>
        /// Helper for creating a new Tag
        /// </summary>
        /// <param name="name">The new tag name</param>
        public static Tag CreateTag(string name)
        {
            if (GetTag(name, false) is Tag tag && tag.TagName == name)
            {
                SL.Log($"Error: A tag already exists called '{name}'");
            }
            else
            {
                tag = new Tag(TagSourceManager.TagRoot, name);
                tag.SetTagType(Tag.TagTypes.Custom);

                TagSourceManager.Instance.DbTags.Add(tag);
                TagSourceManager.Instance.RefreshTags(true);

                SL.Log($"Created a tag, name: {tag.TagName}");
            }

            return tag;
        }

        /// <summary>
        /// Helper to set the TagSource component on an Item, Status etc.
        /// </summary>
        /// <param name="gameObject">The GameObject to add or set the component to.</param>
        /// <param name="tags">The list of tags (Tag Names) to add.</param>
        /// <param name="destroyExisting">Removing existing tags, if any?</param>
        /// <returns>The resulting TagSource component.</returns>
        public static TagListSelectorComponent SetTagSource(GameObject gameObject, string[] tags, bool destroyExisting)
        {
            var tagsource = gameObject.GetComponent<TagListSelectorComponent>();
            if (!tagsource)
                tagsource = gameObject.AddComponent<TagSource>();

            List<TagSourceSelector> list;
            if (destroyExisting)
            {
                list = new List<TagSourceSelector>();
                tagsource.m_tagSelectors = list;
                tagsource.m_tags = new List<Tag>();
            }
            else
                list = tagsource.m_tagSelectors;

            foreach (var name in tags)
            {
                var tag = GetTag(name);
                if (tag != Tag.None)
                    list.Add(new TagSourceSelector(tag));
            }

            tagsource.RefreshTags();

            return tagsource;
        }
    }
}
