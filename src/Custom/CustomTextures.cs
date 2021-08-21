using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SLShaderDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, SideLoader.CustomTextures.ShaderPropType>>;

namespace SideLoader
{
    /// <summary>
    /// SideLoader's helper class for working with Texture2Ds.
    /// </summary>
    public class CustomTextures
    {
        /// <summary>
        /// Public dictionary of textures being used for global replacements.
        /// </summary>
        public static Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Helper enum for certain types of icon borders that Nine Dots use.
        /// </summary>
        public enum SpriteBorderTypes
        {
            /// <summary>No icon border.</summary>
            NONE,
            /// <summary>The border for Item Icons.</summary>
            ItemIcon,
            /// <summary>The border for Skill Tree Icons.</summary>
            SkillTreeIcon,
        }

        /// <summary>
        /// Handles how different types of Textures are loaded with Texture2D.LoadImage.
        /// If it's not a Normal (bump map) or GenTex, just use Default.
        /// </summary>
        public enum TextureType
        {
            /// <summary>No special behaviour applied to the Texture.</summary>
            Default,
            /// <summary>For Normal Map (bump map) textures.</summary>
            Normal,
            /// <summary>For GenTex (Generative Texture), for Nine Dots' shader.</summary>
            GenTex
        }

        internal static void Init()
        {
            QualitySettings.masterTextureLimit = 0;

            SL.OnPacksLoaded += ReplaceActiveTextures;
            SL.OnSceneLoaded += ReplaceActiveTextures;
        }

        /// <summary>
        /// Simple helper for loading a Texture2D from a .png filepath
        /// </summary>
        /// <param name="filePath">The full or relative filepath</param>
        /// <param name="mipmap">Do you want mipmaps for this texture?</param>
        /// <param name="linear">Is this linear or sRGB? (Normal or non-normal)</param>
        /// <returns>The Texture2D (or null if there was an error)</returns>
        public static Texture2D LoadTexture(string filePath, bool mipmap, bool linear)
        {
            if (!File.Exists(filePath))
                return null;

            var file = File.ReadAllBytes(filePath);

            return LoadTexture(file, mipmap, linear);
        }

        public static Texture2D LoadTexture(byte[] data, bool mipmap, bool linear)
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.DXT5, mipmap, linear);

            try
            {
                tex.LoadImage(data);
            }
            catch (Exception e)
            {
                SL.Log("Error loading texture! Message: " + e.Message + "\r\nStack: " + e.StackTrace);
            }

            tex.filterMode = FilterMode.Bilinear;

            return tex;
        }

        /// <summary> Helper for creating a generic sprite with no border, from a Texture2D. Use CustomTextures.LoadTexture() to load a tex from a filepath. </summary>
        public static Sprite CreateSprite(Texture2D texture)
        {
            return CreateSprite(texture, SpriteBorderTypes.NONE);
        }

        /// <summary> Create a sprite with the appropriate border for the type. Use CustomTextures.LoadTexture() to load a tex from a filepath.</summary>
        public static Sprite CreateSprite(Texture2D tex, SpriteBorderTypes borderType)
        {
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;

            Vector4 offset = Vector4.zero;
            switch (borderType)
            {
                case SpriteBorderTypes.ItemIcon:
                    offset = new Vector4(1, 2, 2, 3); break;
                case SpriteBorderTypes.SkillTreeIcon:
                    offset = new Vector4(1, 1, 1, 2); break;
                default: break;
            }

            var rect = new Rect(
                offset.x,
                offset.z,
                tex.width - offset.y,
                tex.height - offset.w);

            return Sprite.Create(tex, rect, Vector2.zero, 100f, 1, SpriteMeshType.Tight);
        }

        /// <summary>
        /// Save an Icon as a png file.
        /// </summary>
        /// <param name="icon">The icon to save.</param>
        /// <param name="dir">The directory to save at.</param>
        /// <param name="name">The filename of the icon.</param>
        public static void SaveIconAsPNG(Sprite icon, string dir, string name = "icon")
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            SaveTextureAsPNG(icon.texture, dir, name, false);
        }

        public static Texture2D Copy(Texture2D orig, Rect rect)
        {
            Color[] pixels;

            if (!orig.isReadable)
                orig = ForceReadTexture(orig);

            pixels = orig.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);

            var _newTex = new Texture2D((int)rect.width, (int)rect.height);
            _newTex.SetPixels(pixels);

            return _newTex;
        }

        public static Texture2D ForceReadTexture(Texture2D tex)
        {
            try
            {
                FilterMode origFilter = tex.filterMode;
                tex.filterMode = FilterMode.Point;

                var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                rt.filterMode = FilterMode.Point;
                RenderTexture.active = rt;

                Graphics.Blit(tex, rt);

                var _newTex = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);

                _newTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                _newTex.Apply(false, false);

                RenderTexture.active = null;
                tex.filterMode = origFilter;

                return _newTex;
            }
            catch (Exception e)
            {
                SL.Log("Exception on ForceReadTexture: " + e.ToString());
                return default;
            }
        }

        public static void SaveTextureAsPNG(Texture2D tex, string dir, string name, bool isDTXnmNormal = false)
        {
            if (!tex)
                return;

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            byte[] data;
            string savepath = Path.Combine(dir, $"{name}.png");

            // Make sure we can EncodeToPNG it.
            if (tex.format != TextureFormat.ARGB32 || !tex.isReadable)
            {
                tex = ForceReadTexture(tex);
            }

            if (isDTXnmNormal)
            {
                tex = DTXnmToRGBA(tex);
                tex.Apply(false, false);
            }

            data = tex.EncodeToPNG();

            if (data == null || data.Length < 1)
                SL.Log("Couldn't get any data for the texture!");
            else
                File.WriteAllBytes(savepath, data);
        }

        // Converts DTXnm-format Normal Map to RGBA-format Normal Map.
        public static Texture2D DTXnmToRGBA(Texture2D tex)
        {
            Color[] colors = tex.GetPixels();

            for (int i = 0; i < colors.Length; i++)
            {
                var c = colors[i];

                c.r = c.a * 2 - 1;  // red <- alpha
                c.g = c.g * 2 - 1;  // green is always the same

                var rg = new Vector2(c.r, c.g); //this is the red-green vector
                c.b = Mathf.Sqrt(1 - Mathf.Clamp01(Vector2.Dot(rg, rg))); //recalculate the blue channel

                colors[i] = new Color(
                    (c.r * 0.5f) + 0.5f,
                    (c.g * 0.5f) + 0.25f,
                    (c.b * 0.5f) + 0.5f
                );
            }

            var newtex = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
            newtex.SetPixels(colors);

            return newtex;
        }

        // =========== Shader Properties Helpers ===========

        /// <summary>
        /// Helper enum for the types of Shader Properties which SideLoader supports.
        /// </summary>
        public enum ShaderPropType
        {
            /// <summary>Property Value is a Color</summary>
            Color,
            /// <summary>Property Value is a Vector4</summary>
            Vector,
            /// <summary>Property Value is a float</summary>
            Float
        }

        /// <summary>
        /// Get the Properties for the Shader on the provided material.
        /// </summary>
        /// <param name="mat">The material to get properties for.</param>
        /// <returns>If supported, the list of Shader Properties.</returns>
        public static List<SL_Material.ShaderProperty> GetProperties(Material mat)
        {
            var list = new List<SL_Material.ShaderProperty>();

            if (ShaderPropertyDicts == null)
            {
                SL.Log("Dict is null");
                return list;
            }

            if (ShaderPropertyDicts.ContainsKey(mat.shader.name))
            {
                var dict = ShaderPropertyDicts[mat.shader.name];

                if (dict == null)
                {
                    SL.Log("ShaderProperties for material " + mat.shader.name + " is in main dict, but Property Dict is null");
                    return list;
                }
                else
                {
                    foreach (var entry in dict)
                    {
                        switch (entry.Value)
                        {
                            case ShaderPropType.Color:
                                list.Add(new SL_Material.ColorProp()
                                {
                                    Name = entry.Key,
                                    Value = mat.GetColor(entry.Key)
                                });
                                break;
                            case ShaderPropType.Float:
                                list.Add(new SL_Material.FloatProp()
                                {
                                    Name = entry.Key,
                                    Value = mat.GetFloat(entry.Key)
                                });
                                break;
                            case ShaderPropType.Vector:
                                list.Add(new SL_Material.VectorProp()
                                {
                                    Name = entry.Key,
                                    Value = mat.GetVector(entry.Key)
                                });
                                break;
                        }
                    }
                }
            }
            else
            {
                //SL.Log("Shader GetProperties not supported: " + mat.shader.name);
            }

            return list;
        }

        public const string CUSTOM_MAINSET_MAINSTANDARD = "Custom/Main Set/Main Standard";
        public const string CUSTOM_DISTORT_DISTORTTEXTURESPEC = "Custom/Distort/DistortTextureSpec";

        /// <summary>
        /// Keys: see the CustomTextures.CUSTOM_ const strings, Values: Shader Property names and types.
        /// </summary>
        private static readonly SLShaderDict ShaderPropertyDicts = new SLShaderDict()
        {
            {
                CUSTOM_MAINSET_MAINSTANDARD,
                new Dictionary<string, ShaderPropType>
                {
                    { "_Color",                 ShaderPropType.Color },
                    { "_Cutoff",                ShaderPropType.Float },
                    { "_Dither",                ShaderPropType.Float },
                    { "_DoubleFaced",           ShaderPropType.Float },
                    { "_NormStr",               ShaderPropType.Float },
                    { "_SpecColor",             ShaderPropType.Color },
                    { "_SmoothMin",             ShaderPropType.Float },
                    { "_SmoothMax",             ShaderPropType.Float },
                    { "_OccStr",                ShaderPropType.Float },
                    { "_EmissionColor",         ShaderPropType.Color },
                    { "_EmitAnimSettings",      ShaderPropType.Vector },
                    { "_EmitScroll",            ShaderPropType.Float },
                    { "_EmitPulse",             ShaderPropType.Float },
                    { "_DetColor",              ShaderPropType.Color },
                    { "_DetTiling",             ShaderPropType.Vector },
                    { "_DetNormStr",            ShaderPropType.Float },
                    { "_VPRTexColor",           ShaderPropType.Color },
                    { "_VPRTexSettings",        ShaderPropType.Vector },
                    { "_VPRSpecColor",          ShaderPropType.Color },
                    { "_VPRNormStr",            ShaderPropType.Float },
                    { "_VPRUnderAuto",          ShaderPropType.Float },
                    { "_VPRTiling",             ShaderPropType.Float },
                    { "_AutoTexColor",          ShaderPropType.Color },
                    { "_AutoTexSettings",       ShaderPropType.Vector },
                    { "_AutoTexHideEmission",   ShaderPropType.Float },
                    { "_AutoSpecColor",         ShaderPropType.Color },
                    { "_AutoNormStr",           ShaderPropType.Float },
                    { "_AutoTexTiling",         ShaderPropType.Float },
                    { "_SnowEnabled",           ShaderPropType.Float }
                }
            },
            {
                CUSTOM_DISTORT_DISTORTTEXTURESPEC,
                new Dictionary<string, ShaderPropType>
                {
                    { "_Color",             ShaderPropType.Color },
                    { "_SpecColor",         ShaderPropType.Color },
                    { "_NormalStrength",    ShaderPropType.Float },
                    { "_Speed",             ShaderPropType.Float },
                    { "_Scale",             ShaderPropType.Float },
                    { "_MaskPow",           ShaderPropType.Float },
                }
            }
        };

        // ============= GLOBAL TEXTURE REPLACEMENT ===============

        /// <summary>
        /// Internal method used to replace active textures from our Textures dictionary.
        /// </summary>
        public static void ReplaceActiveTextures()
        {
            if (Textures.Count < 1)
            {
                return;
            }

            float start = Time.realtimeSinceStartup;
            SL.Log("Replacing active textures.");

            int replaced = 0;

            // ============ Materials ============

            var list = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var mat in list)
            {
                var texNames = mat.GetTexturePropertyNames();

                foreach (var layer in texNames)
                {
                    if (mat.GetTexture(layer) is Texture tex && Textures.ContainsKey(tex.name))
                    {
                        // SL.Log("Replacing layer " + layer + " on material " + mat.name);
                        mat.SetTexture(layer, Textures[tex.name]);

                        replaced++;
                    }
                }
            }

            // ============ UI.Image ============ //

            //var images = Resources.FindObjectsOfTypeAll<Image>().Where(x => x.sprite != null && x.sprite.texture != null);

            //foreach (Image i in images)
            //{
            //    if (Textures.ContainsKey(i.sprite.texture.name))
            //    {
            //        SL.Log("Replacing sprite for " + i.name);
            //        i.sprite = CreateSprite(Textures[i.sprite.texture.name]);
            //    }
            //}

            var time = Math.Round(1000f * (Time.realtimeSinceStartup - start), 2);

            SL.Log($"Replaced {replaced} textures, took {time} ms");
        }
    }
}