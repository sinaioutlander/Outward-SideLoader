using BepInEx;
using SideLoader.SLPacks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SLPackContent = System.Collections.Generic.Dictionary<System.Type, System.Collections.Generic.Dictionary<string, object>>;

namespace SideLoader
{
    public class SLPack
    {
        /// <summary>The unique Name of this SLPack</summary>
        public string Name { get; internal set; }

        public string ActualFolderName => s_actualFolderName ?? Name;
        internal string s_actualFolderName;

        /// <summary>
        /// Is this pack in the legacy 'Mods\SideLoader\{name}\' format?
        /// </summary>
        public bool IsInLegacyFolder { get; internal set; }

        /// <summary>
        /// Returns the folder path for this SL Pack.
        /// </summary>
        public virtual string FolderPath => !IsInLegacyFolder
            ? Path.Combine(Paths.PluginPath, ActualFolderName, "SideLoader")
            : Path.Combine(SL.LEGACY_SL_FOLDER, ActualFolderName);

        // Hard-coded dictionaries for easy access:

        /// <summary>AssetBundles loaded from the `AssetBundles\` folder. Dictionary Key is the file name.</summary>
        public Dictionary<string, AssetBundle> AssetBundles = new Dictionary<string, AssetBundle>();
        /// <summary>Texture2Ds loaded from the PNGs in the `Texture2D\` folder (not from the `Items\...` folders). Dictionary Key is the file name (without ".png")</summary>
        public Dictionary<string, Texture2D> Texture2D = new Dictionary<string, Texture2D>();
        /// <summary>AudioClips loaded from the WAV files in the `AudioClip\` folder. Dictionary Key is the file name (without ".wav")</summary>
        public Dictionary<string, AudioClip> AudioClips = new Dictionary<string, AudioClip>();
        /// <summary>SL_Characters loaded from the `Characters\` folder. Dictionary Key is the character UID.</summary>
        public Dictionary<string, SL_Character> CharacterTemplates = new Dictionary<string, SL_Character>();

        /// <summary>
        /// SLPackBundles loaded from the `PackBundles\` folder. Key is the file name of the bundle, including any extension if provided.
        /// </summary>
        public readonly Dictionary<string, SLPackBundle> PackBundles = new Dictionary<string, SLPackBundle>();

        internal SLPackContent LoadedContent = new SLPackContent();

        // ============ FILE IO ============ //

        /// <summary>
        /// Returns the folder path for this SLPack, plus the given SLPackCategory's FolderPath.
        /// </summary>
        /// <typeparam name="T">The SLPackCategory type.</typeparam>
        /// <returns>The path string, or null if an error was experienced.</returns>
        public string GetPathForCategory<T>() where T : SLPackCategory
            => GetPathForCategory(typeof(T));

        /// <summary>
        /// Returns the folder path for this SLPack, plus the given SLPackCategory's FolderPath.
        /// </summary>
        /// <param name="type">The SLPackCategory type.</param>
        /// <returns>The path string, or null if an error was experienced.</returns>
        public string GetPathForCategory(Type type)
        {
            if (type == null || !typeof(SLPackCategory).IsAssignableFrom(type))
                throw new ArgumentException("type");

            var instance = SLPackManager.GetCategoryInstance(type);
            if (instance == null)
                throw new Exception($"Trying to get folder path for '{type.FullName}', but category instance is null!");

            return Path.Combine(this.FolderPath, instance.FolderName);
        }

        public virtual bool DirectoryExists(string relativeDirectory)
            => Directory.Exists(relativeDirectory);

        public virtual bool FileExists(string relativeDirectory, string file)
            => File.Exists(Path.Combine(relativeDirectory, file));

        public virtual string[] GetDirectories(string relativeDirectory)
            => Directory.GetDirectories(relativeDirectory);

        public virtual string[] GetFiles(string relativeDirectory)
            => Directory.GetFiles(relativeDirectory);

        public string[] GetFiles(string relativeDirectory, string endsWith)
        {
            var files = GetFiles(relativeDirectory);

            return files.Where(it => it.EndsWith(endsWith, StringComparison.InvariantCultureIgnoreCase))
                        .ToArray();
        }

        protected internal virtual T ReadXmlDocument<T>(string relativeDirectory, string file)
        {
            return (T)Serializer.LoadFromXml(Path.Combine(relativeDirectory, file));
        }

        protected internal virtual AssetBundle LoadAssetBundle(string relativeDirectory, string file)
        {
            var path = Path.Combine(relativeDirectory, file);

            if (!File.Exists(path))
                return null;

            return AssetBundle.LoadFromFile(path);
        }

        protected internal virtual void LoadAudioClip(string relativeDirectory, string file, Action<AudioClip> onClipLoaded)
        {
            var path = Path.Combine(relativeDirectory, file);

            if (!File.Exists(path))
                return;

            CustomAudio.LoadAudioClip(path, this, (AudioClip clip) => { onClipLoaded?.Invoke(clip); });
        }

        protected internal virtual Texture2D LoadTexture2D(string relativeDirectory, string file, bool mipmap = false, bool linear = false)
        {
            var path = Path.Combine(relativeDirectory, file);

            if (!File.Exists(path))
                return null;

            return CustomTextures.LoadTexture(path, mipmap, linear);
        }

        // ============================================= //

        /// <summary>
        /// Get content of type T by the file name. If there are duplicates, it will return the first found (unknown order).<br/><br/>
        /// Note: This will <b>not</b> find "extra" assets such as the 'icon.png' and Textures\ files from an SL_Item template, etc.
        /// </summary>
        /// <typeparam name="T">The type of content you're looking for</typeparam>
        /// <param name="fileName">The content's file name (without extension)</param>
        /// <param name="stringComparison">The string comparison type to use, default is to culture- and case-insensitive.</param>
        /// <returns></returns>
        public virtual T GetContentByFileName<T>(string fileName, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            var content = GetContentOfType<T>();

            if (content == null || !content.Any())
                return default;

            var query = content.Where(it => string.Equals(Path.GetFileNameWithoutExtension(it.Key), fileName, stringComparison))
                               .Select(it => it.Value);

            return query.FirstOrDefault();
        }

        /// <summary>
        /// Get all loaded content of type T from the SL Pack. The return dictionary keys are the file paths.<br/><br/>
        /// Note: This will <b>not</b> find "extra" assets such as the 'icon.png' and Textures\ files from an SL_Item template, etc.
        /// </summary>
        /// <typeparam name="T">The type of content to load from the SL Pack.</typeparam>
        /// <returns>A dictionary of the loaded content if any found, otherwise null. Keys are the file paths, Values are the content.</returns>
        public virtual Dictionary<string, T> GetContentOfType<T>()
        {
            var ret = new Dictionary<string, T>();

            foreach (var ctg in LoadedContent)
            {
                foreach (var entry in ctg.Value)
                {
                    if (typeof(T).IsAssignableFrom(entry.Value.GetType()))
                        ret.Add(entry.Key, (T)entry.Value);
                }
            }

            return ret;
        }

        /// <summary>
        /// Get the loaded content from this SL Pack from the given category. Categories are in the "SideLoader.SLPacks.Categories" namespace.<br/><br/>
        /// Note: This will <b>not</b> find "extra" assets such as the 'icon.png' and Textures\ files from an SL_Item template, etc.
        /// </summary>
        /// <typeparam name="T">The SLPackCategory type to get content for.</typeparam>
        /// <returns>A dictionary of the loaded content if any found, otherwise null. Keys are the file paths, Values are the content.</returns>
        public Dictionary<string, object> GetContentForCategory<T>() where T : SLPackCategory
            => GetContentForCategory(typeof(T));

        /// <summary>
        /// Get the loaded content from this SL Pack from the given category. Categories are in the "SideLoader.SLPacks.Categories" namespace.<br/><br/>
        /// Note: This will <b>not</b> find "extra" assets such as the 'icon.png' and Textures\ files from an SL_Item template, etc.
        /// </summary>
        /// <param name="type">The SLPackCategory type to get content for.</param>
        /// <returns>A dictionary of the loaded content if any found, otherwise null. Keys are the file paths, Values are the content.</returns>
        public virtual Dictionary<string, object> GetContentForCategory(Type type)
        {
            if (type == null || !typeof(SLPackCategory).IsAssignableFrom(type))
                throw new ArgumentException("type");

            if (LoadedContent == null)
                return null;

            LoadedContent.TryGetValue(type, out Dictionary<string, object> ret);

            return ret;
        }

        internal void TryApplyItemTextureBundles()
        {
            var bundlesFolder = Path.Combine(this.FolderPath, "Items", "TextureBundles");
            if (Directory.Exists(bundlesFolder))
            {
                foreach (var file in Directory.GetFiles(bundlesFolder))
                {
                    if (AssetBundle.LoadFromFile(file) is AssetBundle bundle)
                    {
                        CustomItemVisuals.ApplyTexturesFromAssetBundle(bundle);
                    }
                }
            }
        }
    }
}
