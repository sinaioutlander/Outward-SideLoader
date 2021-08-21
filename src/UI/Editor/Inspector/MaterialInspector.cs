using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SideLoader.UI.Editor
{
    public class MaterialInspector : ReflectionInspector
    {
        public SL_Material RefMaterial;

        public MaterialInspector(object target) : base(target)
        {
            RefMaterial = target as SL_Material;
        }

        internal void Save()
        {
            var path = this.RefMaterial.m_serializedFolderPath;

            Serializer.SaveToXml(path, "properties", Target);
        }

        private void Load()
        {
            var path = Path.Combine(this.RefMaterial.m_serializedFolderPath, "properties.xml");

            if (Serializer.LoadFromXml(path) is SL_Material loadedData)
            {
                SL.Log("Loaded SL_Material data, replacing");
                CopyValuesFrom(loadedData);
            }
        }

        internal Text m_fullPathLabel;

        internal void ConstructMaterialUI()
        {
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

            var fullLabel = UIFactory.CreateInputField(fullPathRowObj);
            var fullText = fullLabel.GetComponent<Text>();
            fullText.text = $"<b>XML Path:</b> {Path.Combine(RefMaterial.m_serializedFolderPath, "properties.xml")}";
            var fulllayout = fullLabel.AddComponent<LayoutElement>();
            fulllayout.minWidth = 130;
            fulllayout.flexibleWidth = 0;

            m_fullPathLabel = fullText;

            // ========= Save/Load Row =========

            var saveLoadRowObj = UIFactory.CreateHorizontalGroup(vertGroupObj, new Color(1, 1, 1, 0));
            var saveLoadGroup = saveLoadRowObj.GetComponent<HorizontalLayoutGroup>();
            saveLoadGroup.spacing = 5;
            saveLoadGroup.padding = new RectOffset(3, 3, 3, 3);
            saveLoadGroup.childForceExpandWidth = true;
            saveLoadGroup.childForceExpandHeight = true;

            var saveBtnObj = UIFactory.CreateButton(saveLoadRowObj, new Color(0.2f, 0.5f, 0.2f));
            var btnLayout = saveBtnObj.AddComponent<LayoutElement>();
            btnLayout.minHeight = 25;
            var saveBtnText = saveBtnObj.GetComponentInChildren<Text>();
            saveBtnText.text = "Save";
            var saveBtn = saveBtnObj.GetComponent<Button>();
            saveBtn.onClick.AddListener(() =>
            {
                Save();
            });

            var loadBtnObj = UIFactory.CreateButton(saveLoadRowObj, new Color(0.5f, 0.3f, 0.2f));
            var loadLayout = loadBtnObj.AddComponent<LayoutElement>();
            loadLayout.minHeight = 25;
            var loadBtnText = loadBtnObj.GetComponentInChildren<Text>();
            loadBtnText.text = "Load";
            var loadBtn = loadBtnObj.GetComponent<Button>();
            loadBtn.onClick.AddListener(() =>
            {
                Load();
            });
        }
    }
}
