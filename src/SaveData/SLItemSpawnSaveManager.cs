using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace SideLoader.SaveData
{
    public static class SLItemSpawnSaveManager
    {
        public struct ItemSpawnInfo
        {
            public string SpawnIdentifier;
            public string ItemUID;
            public int ItemID;
        }

        internal static string GetCurrentSavePath()
        {
            string folder = SLSaveManager.GetSaveFolderForWorldHost();
            if (string.IsNullOrEmpty(folder))
                throw new Exception("Trying to save world host SL_ItemSpawns, but couldn't get a folder!");

            return Path.Combine(folder, SLSaveManager.ITEMSPAWNS_FOLDER, $"{SceneManager.GetActiveScene().name}.itemdata");
        }

        internal static void SaveItemSpawns()
        {
            //SL.LogWarning("~~~~~~~~~~ Saving Item Spawns ~~~~~~~~~~");
            //SL.Log(SceneManager.GetActiveScene().name);

            var savePath = GetCurrentSavePath();

            if (File.Exists(savePath))
                File.Delete(savePath);

            if (SL_ItemSpawn.s_activeSavableSpawns == null || SL_ItemSpawn.s_activeSavableSpawns.Count < 1)
                return;

            SL.Log("Saving " + SL_ItemSpawn.s_activeSavableSpawns.Count + " item spawns");

            using (var file = File.Create(savePath))
            {
                var serializer = Serializer.GetXmlSerializer(typeof(ItemSpawnInfo[]));
                serializer.Serialize(file, SL_ItemSpawn.s_activeSavableSpawns.ToArray());
            }
        }

        internal static Dictionary<string, ItemSpawnInfo> LoadItemSpawnData()
        {
            var savePath = GetCurrentSavePath();

            if (!File.Exists(savePath))
                return null;

            using (var file = File.OpenRead(savePath))
            {
                var serializer = Serializer.GetXmlSerializer(typeof(ItemSpawnInfo[]));

                var dict = new Dictionary<string, ItemSpawnInfo>();

                if (serializer.Deserialize(file) is ItemSpawnInfo[] array)
                {
                    foreach (var entry in array)
                    {
                        if (SL_ItemSpawn.s_registeredSpawnSources.ContainsKey(entry.SpawnIdentifier))
                        {
                            dict.Add(entry.SpawnIdentifier, entry);
                        }
                    }
                }

                return dict;
            }
        }
    }
}
