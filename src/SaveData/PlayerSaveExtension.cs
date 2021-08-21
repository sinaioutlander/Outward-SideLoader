using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace SideLoader.SaveData
{
    [SL_Serialized]
    public abstract class PlayerSaveExtension
    {
        // ~~~~~~ Static ~~~~~~

        [XmlIgnore] internal static HashSet<Type> s_registeredModels = new HashSet<Type>();

        internal static void LoadExtensionTypes()
        {
            s_registeredModels = At.GetImplementationsOf(typeof(PlayerSaveExtension));
        }

        public static string GetSaveFilePath<T>(string characterUID)
        {
            return Path.Combine(SLSaveManager.GetSaveFolderForCharacter(characterUID), 
                SLSaveManager.CUSTOM_FOLDER,
                $"{typeof(T).FullName}.xml");
        }

        /// <summary>
        /// Helper to manually try to load the saved data for an extension of type T, with the given character UID.
        /// </summary>
        /// <typeparam name="T">The type of PlayerSaveExtension you're looking for</typeparam>
        /// <param name="characterUID">The saved character's UID</param>
        /// <returns>The loaded extension data, if found, otherwise null.</returns>
        public static T TryLoadExtension<T>(string characterUID) where T : PlayerSaveExtension
        {
            try
            {
                if (string.IsNullOrEmpty(characterUID))
                    throw new ArgumentNullException("characterUID");

                var filePath = GetSaveFilePath<T>(characterUID);

                T ret = null;
                if (File.Exists(filePath))
                {
                    var serializer = Serializer.GetXmlSerializer(typeof(T));
                    using (var file = File.OpenRead(filePath))
                    {
                        ret = (T)serializer.Deserialize(file);
                    }
                }
                else
                    SL.LogWarning("No extension data found at '" + filePath + "'");

                return ret;
            }
            catch (Exception ex)
            {
                SL.LogWarning($"Exception loading save extension '{typeof(T).FullName}' for character '{characterUID}'");
                SL.Log(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Helper to manually save a PlayerSaveExtension of type T to the given character UID folder.
        /// </summary>
        /// <typeparam name="T">The type of extension you want to save.</typeparam>
        /// <param name="characterUID">The character UID you want to save to</param>
        /// <param name="extension">The extension data to save</param>
        public static void TrySaveExtension<T>(string characterUID, T extension) where T : PlayerSaveExtension
        {
            try
            {
                if (extension == null)
                    throw new ArgumentNullException("extension");

                if (string.IsNullOrEmpty(characterUID))
                    throw new ArgumentNullException("characterUID");

                var filePath = GetSaveFilePath<T>(characterUID);

                if (File.Exists(filePath))
                    File.Delete(filePath);

                var serializer = Serializer.GetXmlSerializer(typeof(T));
                using (var file = File.Create(filePath))
                {
                    serializer.Serialize(file, extension);
                }
            }
            catch (Exception ex)
            {
                SL.LogWarning($"Exception saving save extension '{typeof(T).FullName}' for character '{characterUID}'");
                SL.Log(ex.ToString());
            }
        }

        // Internal load all extensions
        internal static void LoadExtensions(Character character)
        {
            var dir = Path.Combine(SLSaveManager.GetSaveFolderForCharacter(character), SLSaveManager.CUSTOM_FOLDER);

            bool isWorldHost = character.UID == CharacterManager.Instance.GetWorldHostCharacter()?.UID;

            foreach (var file in Directory.GetFiles(dir))
            {
                try
                {
                    var typename = Path.GetFileNameWithoutExtension(file);

                    var type = s_registeredModels.FirstOrDefault(it => it.FullName == typename);
                    if (type != null)
                    {
                        using (var xml = File.OpenRead(file))
                        {
                            var serializer = Serializer.GetXmlSerializer(type);

                            if (serializer.Deserialize(xml) is PlayerSaveExtension loaded)
                                loaded.LoadSaveData(character, isWorldHost);
                            else
                                throw new Exception("Unknown - extension was null after attempting to load XML!");
                        }
                    }
                    else
                        SL.LogWarning("Loading PlayerSaveExtensions, could not find a matching registered type for " + typename);
                }
                catch (Exception ex)
                {
                    SL.LogWarning($"Exception loading Player Save Extension XML from '{file}'!");
                    SL.LogInnerException(ex);
                }
            }
        }

        // Internal save all extensions
        internal static void SaveAllExtensions(Character character, bool isWorldHost)
        {
            var baseDir = Path.Combine(SLSaveManager.GetSaveFolderForCharacter(character), SLSaveManager.CUSTOM_FOLDER);

            foreach (var type in s_registeredModels)
            {
                try
                {
                    PlayerSaveExtension model;

                    var path = Path.Combine(baseDir, $"{type.FullName}.xml");
                    if (File.Exists(path))
                    {
                        using (var file = File.OpenRead(path))
                        {
                            var serializer = Serializer.GetXmlSerializer(type);
                            model = (PlayerSaveExtension)serializer.Deserialize(file);
                        }
                    }
                    else
                        model = (PlayerSaveExtension)Activator.CreateInstance(type);

                    model.SaveDataFromCharacter(character, isWorldHost);

                    Serializer.SaveToXml(baseDir, model.GetType().FullName, model);
                }
                catch (Exception ex)
                {
                    SL.LogWarning($"Exception saving PlayerSaveExtension type: {type.FullName}!");
                    SL.Log(ex.ToString());
                }
            }
        }

        // ~~~~~~ Instance ~~~~~~

        public abstract void ApplyLoadedSave(Character character, bool isWorldHost);
        public abstract void Save(Character character, bool isWorldHost);

        internal void LoadSaveData(Character character, bool isWorldHost)
        {
            SLPlugin.Instance.StartCoroutine(DelayedLoadCoroutine(character, isWorldHost));
        }

        private IEnumerator DelayedLoadCoroutine(Character character, bool isWorldHost)
        {
            yield return new WaitForSeconds(1.0f);
            try
            {
                ApplyLoadedSave(character, isWorldHost);
            }
            catch (Exception ex)
            {
                SL.LogWarning("Exception loading save data onto character: " + this.ToString());
                SL.LogInnerException(ex);
            }
        }

        internal void SaveDataFromCharacter(Character character, bool isWorldHost)
        {
            try
            {
                Save(character, isWorldHost);
            }
            catch (Exception ex)
            {
                SL.LogWarning("Exception saving data from character: " + this.ToString());
                SL.LogInnerException(ex);
            }
        }
    }
}
