using SideLoader.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SideLoader.SLPacks
{
    public interface ITemplateCategory
    {
        IList Internal_CSharpTemplates { get; }
        IList Internal_AllCurrentTemplates { get; }

        // implicit
        string FolderName { get; }
        int LoadOrder { get; }
        Type BaseContainedType { get; }
    }

    public abstract class SLPackTemplateCategory<T> : SLPackCategory, ITemplateCategory where T : ContentTemplate
    {
        public override Type BaseContainedType => typeof(T);

        public IList Internal_CSharpTemplates => CSharpTemplates;
        public static readonly List<T> CSharpTemplates = new List<T>();

        public IList Internal_AllCurrentTemplates => AllCurrentTemplates;
        public static readonly List<T> AllCurrentTemplates = new List<T>();

        public abstract void ApplyTemplate(ContentTemplate template);

        protected internal override void OnHotReload()
        {
        }

        protected internal override void InternalLoad(List<SLPack> packs, bool isHotReload)
        {
            if (AllCurrentTemplates.Count > 0)
                AllCurrentTemplates.Clear();

            var list = new List<ContentTemplate>();

            // Load SL packs first 

            foreach (var pack in packs)
            {
                DeserializePack(pack, list);

                if (pack.PackBundles != null && pack.PackBundles.Count > 0)
                {
                    foreach (var bundle in pack.PackBundles.Values)
                        DeserializePack(bundle, list);
                }
            }

            // Load CSharp templates

            if (CSharpTemplates != null && CSharpTemplates.Any())
            {
                SL.Log(CSharpTemplates.Count + " registered C# templates found...");
                list.AddRange(CSharpTemplates);
            }

            list = TemplateDependancySolver.SolveDependencies(list);

            foreach (var template in list)
            {
                try
                {
                    ApplyTemplate(template);

                    Internal_AllCurrentTemplates.Add(template);
                }
                catch (Exception ex)
                {
                    SL.LogWarning("Exception applying template!");
                    SL.LogInnerException(ex);
                }
            }

            return;
        }

        private void DeserializePack(SLPack pack, List<ContentTemplate> list)
        {
            try
            {
                var dict = new Dictionary<string, object>();

                var dirPath = pack.GetPathForCategory(this.GetType());

                if (!pack.DirectoryExists(dirPath))
                    return;

                // load root directory templates
                foreach (var filepath in pack.GetFiles(dirPath, ".xml"))
                    DeserializeTemplate(dirPath, filepath);

                // load one-subfolder templates
                foreach (var subDir in pack.GetDirectories(dirPath))
                    foreach (var filepath in pack.GetFiles(subDir, ".xml"))
                        DeserializeTemplate(subDir, filepath, subDir);

                AddToSLPackDictionary(pack, dict);

                void DeserializeTemplate(string directory, string filepath, string subfolder = null)
                {
                    var template = pack.ReadXmlDocument<T>(directory, Path.GetFileName(filepath));

                    dict.Add(filepath, template);
                    list.Add(template);

                    template.SerializedSLPackName = pack.Name;
                    template.SerializedFilename = Path.GetFileNameWithoutExtension(filepath);

                    if (!string.IsNullOrEmpty(subfolder))
                        template.SerializedSubfolderName = Path.GetFileName(subfolder);
                }
            }
            catch (Exception ex)
            {
                SL.LogWarning("Exception loading " + this.FolderName + " from '" + pack.Name + "'");
                SL.LogInnerException(ex);
            }
        }
    }
}