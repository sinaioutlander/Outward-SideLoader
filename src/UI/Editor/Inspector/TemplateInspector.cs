using SideLoader.Model;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SideLoader.UI.Editor
{
    public class TemplateInspector : ReflectionInspector
    {
        public override string TabLabel => $" <color=cyan>[T]</color> {this.Template.SerializedFilename} ({base.TabLabel})";

        internal ContentTemplate Template;

        public SLPack RefPack;

        internal void UpdateFullPathText()
        {
            string path;
            if (RefPack.IsInLegacyFolder)
                path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Mods", "SideLoader", RefPack.Name, Template.PackCategory.FolderName);
            else
                path = Path.Combine(BepInEx.Paths.PluginPath, RefPack.Name, "SideLoader", Template.PackCategory.FolderName);

            if (!string.IsNullOrEmpty(this.Template.SerializedSubfolderName))
                path = Path.Combine(path, Template.SerializedSubfolderName);

            m_fullPathLabel.text = Path.GetFullPath(Path.Combine(path, $"{Template.SerializedFilename}.xml"));
        }

        public TemplateInspector(object target, SLPack pack) : base(target)
        {
            Template = target as ContentTemplate;
            RefPack = pack;
        }

        internal override void ChangeType(Type newType)
        {
            var origPack = RefPack.Name;
            var origSub = this.Template.SerializedSubfolderName;
            var origFile = this.Template.SerializedFilename;

            base.ChangeType(newType);

            Template = Target as ContentTemplate;

            Template.SerializedSLPackName = origPack;
            Template.SerializedSubfolderName = origSub;
            Template.SerializedFilename = origFile;

            UpdateFullPathText();
        }

        internal override void CopyValuesFrom(object data)
        {
            var origPack = RefPack.Name;
            var origSub = this.Template.SerializedSubfolderName;
            var origFile = this.Template.SerializedFilename;

            At.CopyFields(Target, data, null, true);
            UpdateValues();

            //base.CopyValuesFrom(data);

            Template = Target as ContentTemplate;

            Template.SerializedFilename = origFile;
            Template.SerializedSubfolderName = origSub;
            Template.SerializedSLPackName = origPack;
        }

        internal void Save()
        {
            if (RefPack == null)
            {
                SL.LogWarning("Error - RefPack is null on TemplateInspector.Save!");
                return;
            }

            var directory = RefPack.GetPathForCategory(Template.PackCategory.GetType());

            if (Template.TemplateAllowedInSubfolder && !string.IsNullOrEmpty(this.Template.SerializedSubfolderName))
                directory = Path.Combine(directory, Template.SerializedSubfolderName);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            Serializer.SaveToXml(directory, this.Template.SerializedFilename, Target);
        }

        private void Load()
        {
            if (RefPack == null)
            {
                SL.LogWarning("Error - RefPack is null on TemplateInspector.Load!");
                return;
            }

            var directory = RefPack.GetPathForCategory(Template.PackCategory.GetType());

            if (Template.TemplateAllowedInSubfolder && !string.IsNullOrEmpty(this.Template.SerializedSubfolderName))
                directory = Path.Combine(directory, Template.SerializedSubfolderName);

            var path = Path.Combine(directory, $"{Template.SerializedFilename}.xml");
            if (!File.Exists(path))
            {
                SL.LogWarning("No file exists at " + path);
                return;
            }

            if (Serializer.LoadFromXml(path) is ContentTemplate loadedData)
            {
                var loadedType = loadedData.GetType();
                SL.Log("Loaded xml, loaded type is " + loadedType);

                if (loadedType != m_targetType)
                    ChangeType(loadedType);

                CopyValuesFrom(loadedData);
            }

            UpdateFullPathText();
        }

        // =========== UI ===============

        internal InputField m_fullPathLabel;

        public void ConstructTemplateUI()
        {
            if (Template == null)
            {
                SL.LogWarning("Template is null!");
                return;
            }

            var vertGroupObj = UIFactory.CreateVerticalGroup(this.Content, new Color(0.07f, 0.07f, 0.07f));
            var vertGroup = vertGroupObj.GetComponent<VerticalLayoutGroup>();
            vertGroup.spacing = 4f;
            vertGroup.padding = new RectOffset
            {
                bottom = 4,
                top = 4,
                right = 4,
                left = 4
            };

            // ========= Full path Row =========

            var fullPathRowObj = UIFactory.CreateHorizontalGroup(vertGroupObj, new Color(1, 1, 1, 0));
            var fullPathGroup = fullPathRowObj.GetComponent<HorizontalLayoutGroup>();
            fullPathGroup.childForceExpandWidth = true;
            fullPathGroup.spacing = 5;

            var fullText = UIFactory.CreateLabel(fullPathRowObj, TextAnchor.MiddleLeft).GetComponent<Text>();
            fullText.text = "XML Path:";
            var fulllayout = fullText.gameObject.AddComponent<LayoutElement>();
            fulllayout.minWidth = 70;
            fulllayout.flexibleWidth = 0;
            var fullLabel = UIFactory.CreateInputField(fullPathRowObj).GetComponent<InputField>();
            fullLabel.readOnly = true;
            m_fullPathLabel = fullLabel;

            // ========= Save/Load Row =========

            var saveLoadRowObj = UIFactory.CreateHorizontalGroup(vertGroupObj, new Color(1, 1, 1, 0));
            var saveLoadGroup = saveLoadRowObj.GetComponent<HorizontalLayoutGroup>();
            saveLoadGroup.spacing = 5;
            saveLoadGroup.padding = new RectOffset(3, 3, 3, 3);
            saveLoadGroup.childForceExpandWidth = true;
            saveLoadGroup.childForceExpandHeight = true;

            var saveBtnObj = UIFactory.CreateButton(saveLoadRowObj, new Color(0.2f, 0.5f, 0.2f));
            var saveBtnLayout = saveBtnObj.AddComponent<LayoutElement>();
            saveBtnLayout.minHeight = 25;
            var saveBtnText = saveBtnObj.GetComponentInChildren<Text>();
            saveBtnText.text = "Save XML";
            var saveBtn = saveBtnObj.GetComponent<Button>();
            saveBtn.onClick.AddListener(() =>
            {
                Save();
            });

            var loadBtnObj = UIFactory.CreateButton(saveLoadRowObj, new Color(0.5f, 0.3f, 0.2f));
            var loadLayout = loadBtnObj.AddComponent<LayoutElement>();
            loadLayout.minHeight = 25;
            var loadBtnText = loadBtnObj.GetComponentInChildren<Text>();
            loadBtnText.text = "Load XML";
            var loadBtn = loadBtnObj.GetComponent<Button>();
            loadBtn.onClick.AddListener(() =>
            {
                Load();
            });

            // ========= SL_Item Materials ==========

            if (this.Target is SL_Item itemTemplate)
            {
                if (itemTemplate.m_serializedMaterials?.Count > 0)
                {
                    var materialGroupObj = UIFactory.CreateVerticalGroup(this.Content, new Color(0.1f, 0.1f, 0.1f));
                    var matGroup = materialGroupObj.GetComponent<VerticalLayoutGroup>();
                    matGroup.spacing = 4;
                    matGroup.padding = new RectOffset(3, 3, 3, 3);

                    var matTitleObj = UIFactory.CreateLabel(materialGroupObj, TextAnchor.MiddleLeft);
                    var matTitle = matTitleObj.GetComponent<Text>();
                    matTitle.text = "SL_Materials:";
                    matTitle.fontSize = 15;
                    matTitle.color = Color.cyan;

                    var matRowObj = UIFactory.CreateHorizontalGroup(materialGroupObj, new Color(1, 1, 1, 0));
                    var matRowGroup = matRowObj.GetComponent<HorizontalLayoutGroup>();
                    matRowGroup.spacing = 5;
                    matRowGroup.childForceExpandWidth = false;

                    foreach (var mat in itemTemplate.m_serializedMaterials.Values)
                    {
                        var matBtnObj = UIFactory.CreateButton(matRowObj, new Color(0.2f, 0.4f, 0.4f));
                        var matBtnLayout = matBtnObj.AddComponent<LayoutElement>();
                        matBtnLayout.minHeight = 22;
                        matBtnLayout.minWidth = 200;
                        matBtnLayout.flexibleWidth = 0;

                        var matText = matBtnObj.GetComponentInChildren<Text>();
                        matText.text = mat.Name;

                        var matBtn = matBtnObj.GetComponent<Button>();
                        matBtn.onClick.AddListener(() =>
                        {
                            InspectorManager.Instance.Inspect(mat, null);
                        });
                    }
                }
            }
        }
    }
}
