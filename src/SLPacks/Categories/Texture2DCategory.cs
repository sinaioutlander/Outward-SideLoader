using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SideLoader.SLPacks.Categories
{
    public class Texture2DCategory : SLPackAssetCategory
    {
        public override string FolderName => "Texture2D";

        public override Type BaseContainedType => typeof(Texture2D);

        public override Dictionary<string, object> LoadContent(SLPack pack, bool isHotReload)
        {
            var dict = new Dictionary<string, object>();

            var dirPath = pack.GetPathForCategory<Texture2DCategory>();

            if (!pack.DirectoryExists(dirPath))
                return dict;

            foreach (var texPath in pack.GetFiles(dirPath, ".png"))
            {
                LoadTexture(pack, dict, dirPath, texPath, true);
            }

            var localDir = Path.Combine(dirPath, "Local");
            if (Directory.Exists(localDir))
            {
                foreach (var localTex in pack.GetFiles(localDir, ".png"))
                    LoadTexture(pack, dict, localDir, localTex, false);
            }

            return dict;
        }

        private void LoadTexture(SLPack pack, Dictionary<string, object> dict, string dirPath, string texFile, bool addGlobal)
        {
            var texture = pack.LoadTexture2D(dirPath, texFile, false, false);

            var name = Path.GetFileNameWithoutExtension(texFile);

            // add to the Texture2D dict for this pack
            if (pack.Texture2D.ContainsKey(name))
            {
                SL.LogWarning("Trying to load two textures with the same name into the same SLPack: " + name);
                return;
            }

            pack.Texture2D.Add(name, texture);

            dict.Add(Path.Combine(dirPath, texFile), texture);

            if (addGlobal)
            {
                // add to the global Tex replacements dict
                if (CustomTextures.Textures.ContainsKey(name))
                {
                    SL.Log("Custom Texture2Ds: A Texture already exists in the global list called " + name + "! Overwriting with this one...");
                    CustomTextures.Textures[name] = texture;
                }
                else
                    CustomTextures.Textures.Add(name, texture);
            }
        }

        protected internal override void OnHotReload()
        {
            CustomTextures.Textures.Clear();
        }
    }
}
