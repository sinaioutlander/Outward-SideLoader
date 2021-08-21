using BepInEx;
using SideLoader.Model;
using SideLoader.Model.Status;
using SideLoader.SLPacks;
using SideLoader.SLPacks.Categories;
using SideLoader.UI.Editor;
using SideLoader.UI.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SideLoader.UI.SLPackViewer
{
    public class SLPackListView
    {
        public SLPackListView()
        {
            Instance = this;

            m_currentCategory = SLPackManager.GetCategoryInstance<ItemCategory>();

            var list = At.GetImplementationsOf(typeof(ContentTemplate));
            s_templateTypes = list.OrderBy(it => it.Name).ToList();

            ConstructUI();
        }

        internal static List<Type> s_templateTypes = new List<Type>();
        internal static List<Type> s_currentTemplateTypes = new List<Type>();

        public static SLPackListView Instance;

        internal static Action OnToggleShow;

        internal SLPack m_currentPack;
        internal ITemplateCategory m_currentCategory;
        //internal SLPack.SubFolders m_currentSubfolder = SLPack.SubFolders.Items;
        internal Type m_currentGeneratorType;

        internal ContentAutoCompleter AutoCompleter;

        internal void UpdateAutocompletes()
        {
            AutoCompleter.CheckAutocomplete();
            AutoCompleter.Update();
        }

        public void UseAutocomplete(string idToUse)
        {
            m_generatorTargetInput.text = idToUse;
            //AutoCompleter.ClearAutocompletes();
            AutoCompleter.m_mainObj.SetActive(false);
        }

        public void Init()
        {
            AutoCompleter = new ContentAutoCompleter();
            AutoCompleter.Init();

            RefreshGeneratorTypeDropdown();

            RefreshLoadedSLPacks();
        }

        internal void RefreshLoadedSLPacks()
        {
            m_slPackDropdown.options.Clear();
            m_slPackDropdownLabel.text = "Choose an SLPack...";

            m_slPackDropdown.options.Add(new Dropdown.OptionData
            {
                text = "No SLPack selected"
            });

            for (int i = 0; i < SL.s_packs.Count; i++)
            {
                var pack = SL.s_packs.ElementAt(i).Value;
                m_slPackDropdown.options.Add(new Dropdown.OptionData
                {
                    text = pack.Name
                });
            }

            m_slPackDropdown.value = 1;
            m_slPackDropdown.value = 0;

            m_currentPack = null;
            m_scrollObj.gameObject.SetActive(false);
            m_generateObj.gameObject.SetActive(false);
        }

        internal bool CanSelectedTypeBeInSubfolder()
        {
            var temp = (ContentTemplate)Activator.CreateInstance(m_currentGeneratorType);
            return temp.TemplateAllowedInSubfolder;
        }

        internal bool CanSelectedTypeCloneFromTarget()
        {
            var temp = (ContentTemplate)Activator.CreateInstance(m_currentGeneratorType);
            return temp.CanParseContent;
        }

        internal void ClearPackEntryButtons()
        {
            foreach (Transform child in m_pageContent.transform)
                GameObject.Destroy(child.gameObject);
        }

        internal void SetSLPackFromDowndown(int val)
        {
            if (val < 1 || val > SL.s_packs.Count)
            {
                m_scrollObj.gameObject.SetActive(false);
                m_generateObj.gameObject.SetActive(false);
                return;
            }

            m_currentPack = SL.s_packs.ElementAt(val - 1).Value;
            //m_slPackLabel.text = $"<b>Inspecting:</b> {m_currentPack.Name}";

            m_scrollObj.gameObject.SetActive(true);
            m_generateObj.gameObject.SetActive(true);

            RefreshSLPackContentList();
        }

        internal void RefreshGeneratorTypeDropdown()
        {
            Type baseGeneratorType = m_currentCategory.BaseContainedType;

            if (baseGeneratorType == null)
            {
                SL.LogError("No base type for subfolder: " + m_currentCategory);
                return;
            }

            m_genDropdown.options = new List<Dropdown.OptionData>();
            s_currentTemplateTypes.Clear();

            foreach (var type in s_templateTypes)
            {
                if (!baseGeneratorType.IsAssignableFrom(type))
                    continue;

                s_currentTemplateTypes.Add(type);
                m_genDropdown.options.Add(new Dropdown.OptionData { text = type.Name });
            }

            m_genDropdown.value = 1;
            m_genDropdown.value = 0;

            m_generatorTitle.text = "Template Generator (" + m_currentCategory.FolderName + ")";
        }

        internal void RefreshSLPackContentList()
        {
            if (m_currentPack == null)
                return;

            ClearPackEntryButtons();

            var dict = m_currentPack.GetContentForCategory(m_currentCategory.GetType());
            if (dict == null)
                return;
            foreach (var entry in dict.Values)
                AddSLPackTemplateButton(entry as ContentTemplate);
        }

        private void CreatePack()
        {
            RefreshLoadedSLPacks();

            var name = m_createInput.text;

            var safename = Serializer.ReplaceInvalidChars(name);

            if (string.IsNullOrEmpty(safename))
                return;

            if (name != safename)
            {
                SL.LogWarning("SLPack name contains invalid path characters! Try '" + safename + "'");
                return;
            }

            if (SL.GetSLPack(name) != null)
            {
                SL.LogWarning("Cannot make an SLPack with this name as one already exists!");
                return;
            }

            var slPack = new SLPack
            {
                Name = name
            };

            Directory.CreateDirectory(Path.Combine(Paths.PluginPath, name));
            SL.s_packs.Add(name, slPack);

            RefreshLoadedSLPacks();

            for (int i = 0; i < m_slPackDropdown.options.Count; i++)
            {
                var opt = m_slPackDropdown.options[i];
                if (opt.text == name)
                {
                    m_slPackDropdown.value = i;
                    break;
                }
            }
        }

        internal void BeginConfirmHotReload()
        {
            m_hotReloadRow.transform.GetChild(0).gameObject.SetActive(false);

            var reloadLabelObj = UIFactory.CreateLabel(m_hotReloadRow, TextAnchor.MiddleLeft);
            var reloadText = reloadLabelObj.GetComponent<Text>();
            reloadText.text = "Really reload? This will close all open editor windows!";

            var cancelBtnObj = UIFactory.CreateButton(m_hotReloadRow, new Color(0.2f, 0.2f, 0.2f));
            var cancelLayout = cancelBtnObj.AddComponent<LayoutElement>();
            cancelLayout.minWidth = 80;
            cancelLayout.minHeight = 25;
            cancelLayout.minWidth = 100;
            cancelLayout.flexibleWidth = 0;
            var cancelText = cancelBtnObj.GetComponentInChildren<Text>();
            cancelText.text = "< Cancel";

            var confirmBtnObj = UIFactory.CreateButton(m_hotReloadRow, new Color(1f, 0.3f, 0f));
            var confirmLayout = confirmBtnObj.AddComponent<LayoutElement>();
            confirmLayout.minWidth = 80;
            confirmLayout.minHeight = 25;
            confirmLayout.minWidth = 100;
            confirmLayout.flexibleWidth = 0;
            var confirmText = confirmBtnObj.GetComponentInChildren<Text>();
            confirmText.text = "Confirm";

            var cancelBtn = cancelBtnObj.GetComponent<Button>();
            cancelBtn.onClick.AddListener(() =>
            {
                Close(false);
            });

            var confirmBtn = confirmBtnObj.GetComponent<Button>();
            confirmBtn.onClick.AddListener(() =>
            {
                Close(true);
            });

            void Close(bool confirmed)
            {
                GameObject.Destroy(cancelBtnObj);
                GameObject.Destroy(confirmBtnObj);
                GameObject.Destroy(reloadLabelObj);

                if (confirmed)
                {
                    for (int i = 0; i < InspectorManager.Instance.m_currentInspectors.Count; i++)
                    {
                        InspectorManager.Instance.m_currentInspectors[i].Destroy();
                        i--;
                    }

                    SL.Setup(false);

                    RefreshLoadedSLPacks();
                }

                m_hotReloadRow.transform.GetChild(0).gameObject.SetActive(true);
            }
        }

        #region UI CONSTRUCTION

        private Dropdown m_slPackDropdown;
        private Text m_slPackDropdownLabel;

        private GameObject m_pageContent;

        private GameObject m_hotReloadRow;

        private InputField m_createInput;

        private GameObject m_scrollObj;

        private GameObject m_generateObj;
        private Dropdown m_genDropdown;
        internal InputField m_generatorTargetInput;
        internal Text m_generatorTitle;

        public void ConstructUI()
        {
            GameObject leftPane = UIFactory.CreateVerticalGroup(SLPacksPage.Instance.Content, new Color(72f / 255f, 72f / 255f, 72f / 255f));
            leftPane.name = "SLPack Pane";
            LayoutElement leftLayout = leftPane.AddComponent<LayoutElement>();
            leftLayout.minWidth = 350;
            leftLayout.flexibleWidth = 0;

            VerticalLayoutGroup leftGroup = leftPane.GetComponent<VerticalLayoutGroup>();
            leftGroup.padding.left = 4;
            leftGroup.padding.right = 4;
            leftGroup.padding.top = 8;
            leftGroup.padding.bottom = 4;
            leftGroup.spacing = 4;
            leftGroup.childControlWidth = true;
            leftGroup.childControlHeight = true;
            leftGroup.childForceExpandWidth = true;
            leftGroup.childForceExpandHeight = false;

            ConstructTopArea(leftPane);

            ConstructListView(leftPane);

            ConstructGenerator(leftPane);

            RefreshLoadedSLPacks();
        }

        private void ConstructTopArea(GameObject leftPane)
        {
            GameObject titleObj = UIFactory.CreateLabel(leftPane, TextAnchor.UpperLeft);
            Text titleLabel = titleObj.GetComponent<Text>();
            titleLabel.text = "Active SL Packs";
            titleLabel.fontSize = 20;
            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.minHeight = 30;
            titleLayout.flexibleHeight = 0;

            m_hotReloadRow = UIFactory.CreateVerticalGroup(leftPane, new Color(1, 1, 1, 0));
            var hotReloadGroup = m_hotReloadRow.GetComponent<VerticalLayoutGroup>();
            hotReloadGroup.childForceExpandWidth = true;
            hotReloadGroup.spacing = 5;
            var reloadLayout = m_hotReloadRow.AddComponent<LayoutElement>();
            reloadLayout.minHeight = 25;
            reloadLayout.flexibleHeight = 0;
            var hotReloadBtnObj = UIFactory.CreateButton(m_hotReloadRow, new Color(1f, 0.3f, 0f));
            var refreshLayout = hotReloadBtnObj.AddComponent<LayoutElement>();
            refreshLayout.minHeight = 25;
            var refreshTxt = hotReloadBtnObj.GetComponentInChildren<Text>();
            refreshTxt.text = "Hot Reload";
            var refreshBtn = hotReloadBtnObj.GetComponent<Button>();
            refreshBtn.onClick.AddListener(() =>
            {
                BeginConfirmHotReload();
            });

            var createRow = UIFactory.CreateHorizontalGroup(leftPane, new Color(0.15f, 0.15f, 0.15f));
            var createGroup = createRow.GetComponent<HorizontalLayoutGroup>();
            createGroup.padding = new RectOffset(3, 3, 3, 3);
            createGroup.spacing = 4f;
            createGroup.childForceExpandHeight = true;
            createGroup.childForceExpandWidth = true;
            var createLayout = createRow.AddComponent<LayoutElement>();
            createLayout.minHeight = 30;
            createLayout.flexibleHeight = 0;
            createLayout.flexibleWidth = 9999;

            var createInputObj = UIFactory.CreateInputField(createRow);
            m_createInput = createInputObj.GetComponent<InputField>();
            (m_createInput.placeholder as Text).text = "SLPack name...";
            createInputObj.AddComponent<LayoutElement>().flexibleWidth = 9999;

            var createPackObj = UIFactory.CreateButton(createRow, new Color(0.15f, 0.4f, 0.15f));
            var createBtn = createPackObj.GetComponent<Button>();
            var createBtnLayout = createPackObj.AddComponent<LayoutElement>();
            createBtnLayout.minWidth = 90;
            createBtnLayout.flexibleWidth = 0;
            createBtn.onClick.AddListener(() =>
            {
                CreatePack();
            });
            var createTxt = createPackObj.GetComponentInChildren<Text>();
            createTxt.text = "Create Pack";

            GameObject slPackDropdownObj = UIFactory.CreateDropdown(leftPane, out m_slPackDropdown);
            LayoutElement dropdownLayout = slPackDropdownObj.AddComponent<LayoutElement>();
            dropdownLayout.minHeight = 40;
            dropdownLayout.flexibleHeight = 0;
            dropdownLayout.minWidth = 320;
            dropdownLayout.flexibleWidth = 2;

            m_slPackDropdownLabel = m_slPackDropdown.transform.Find("Label").GetComponent<Text>();
            m_slPackDropdownLabel.text = "Choose an SLPack...";
            m_slPackDropdownLabel.fontSize = 16;
            m_slPackDropdownLabel.color = Color.cyan;

            m_slPackDropdown.onValueChanged.AddListener(SetSLPackFromDowndown);
        }

        private void ConstructListView(GameObject leftPane)
        {
            var titleObj = UIFactory.CreateLabel(leftPane, TextAnchor.MiddleLeft);
            var titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.minHeight = 25;
            var titleText = titleObj.GetComponent<Text>();
            titleText.text = "SLPack Category:";

            var subfolderDropObj = UIFactory.CreateDropdown(leftPane, out Dropdown subDrop);
            var subdropLayout = subfolderDropObj.AddComponent<LayoutElement>();
            subdropLayout.minHeight = 25;

            var list = new List<ITemplateCategory>();

            foreach (var ctg in SLPackManager.SLPackCategories)
            {
                if (ctg is ITemplateCategory category)
                    list.Add(category);
            }

            list = list.OrderBy(it => it.FolderName).ToList();

            subDrop.options = new List<Dropdown.OptionData>();
            foreach (var entry in list)
                subDrop.options.Add(new Dropdown.OptionData { text = entry.FolderName });

            var itemCtg = SLPackManager.GetCategoryInstance<ItemCategory>();

            subDrop.value = list.IndexOf(itemCtg);

            subDrop.onValueChanged.AddListener((int val) =>
            {
                m_currentCategory = list[val];
                RefreshGeneratorTypeDropdown();
                RefreshSLPackContentList();
            });

            m_scrollObj = UIFactory.CreateScrollView(leftPane, out m_pageContent, out SliderScrollbar scroller, new Color(0.1f, 0.1f, 0.1f));
        }

        private void ConstructGenerator(GameObject leftPane)
        {
            m_generateObj = UIFactory.CreateVerticalGroup(leftPane, new Color(0.1f, 0.1f, 0.1f));

            var genLabelObj = UIFactory.CreateLabel(m_generateObj, TextAnchor.MiddleLeft);
            m_generatorTitle = genLabelObj.GetComponent<Text>();
            m_generatorTitle.text = "Template Generator";
            m_generatorTitle.fontSize = 16;

            // Generator dropdown type
            // (callback added below)
            var genDropObj = UIFactory.CreateDropdown(m_generateObj, out m_genDropdown);
            var genDropLayout = genDropObj.AddComponent<LayoutElement>();
            genDropLayout.minHeight = 25;
            var genGroupLayout = m_generateObj.AddComponent<LayoutElement>();
            genGroupLayout.minHeight = 50;
            genGroupLayout.flexibleWidth = 9999;
            var genGroup = m_generateObj.GetComponent<VerticalLayoutGroup>();
            genGroup.childForceExpandHeight = true;
            genGroup.childForceExpandWidth = true;
            genGroup.padding = new RectOffset(3, 3, 3, 3);
            genGroup.spacing = 5;

            // Generator input field for target
            var targetInputFieldObj = UIFactory.CreateInputField(m_generateObj, 14, 3, 0, typeof(AutoCompleteInputField));
            targetInputFieldObj.name = "AutoCompleterInput";
            m_generatorTargetInput = targetInputFieldObj.GetComponent<AutoCompleteInputField>();
            (m_generatorTargetInput.placeholder as Text).text = "Clone target ID";
            var genTargetLayout = m_generatorTargetInput.gameObject.AddComponent<LayoutElement>();
            genTargetLayout.minHeight = 25;

            m_generatorTargetInput.onValueChanged.AddListener((string val) =>
            {
                UpdateAutocompletes();
            });

            // subfolder input field
            var subfolderInputObj = UIFactory.CreateInputField(m_generateObj);
            var subfolderLayout = subfolderInputObj.AddComponent<LayoutElement>();
            subfolderLayout.minHeight = 25;
            var subfolderInput = subfolderInputObj.GetComponent<InputField>();
            (subfolderInput.placeholder as Text).text = "Subfolder (blank for auto)";

            // add this generator callback now that subfolder has been declared
            m_genDropdown.onValueChanged.AddListener((int val) =>
            {
                m_currentGeneratorType = s_currentTemplateTypes[val];

                targetInputFieldObj.SetActive(CanSelectedTypeCloneFromTarget());
                subfolderInputObj.SetActive(CanSelectedTypeBeInSubfolder());
            });

            // name input
            var nameInputObj = UIFactory.CreateInputField(m_generateObj);
            var nameLayout = nameInputObj.AddComponent<LayoutElement>();
            nameLayout.minHeight = 25;
            var nameInput = nameInputObj.GetComponent<InputField>();
            (nameInput.placeholder as Text).text = "Filename (blank for auto)";

            bool exportIconsWanted = true;
            bool exportTexturesWanted = true;

            var toggleIconObj = UIFactory.CreateToggle(m_generateObj, out Toggle toggleIcon, out Text toggleIconTxt);
            var iconLayout = toggleIconObj.AddComponent<LayoutElement>();
            iconLayout.minHeight = 25;
            toggleIconTxt.text = "Export Icons if possible?";
            toggleIcon.onValueChanged.AddListener((bool val) =>
            {
                exportIconsWanted = val;
            });

            var toggleTexObj = UIFactory.CreateToggle(m_generateObj, out Toggle toggleTex, out Text toggleTexText);
            var toggleTexLayout = toggleTexObj.AddComponent<LayoutElement>();
            toggleTexLayout.minHeight = 25;
            toggleTexText.text = "Export Textures if possible?";
            toggleTex.onValueChanged.AddListener((bool val) =>
            {
                exportTexturesWanted = val;
            });

            // generate button
            var genBtnObj = UIFactory.CreateButton(m_generateObj, new Color(0.15f, 0.45f, 0.15f));
            var genBtnLayout = genBtnObj.AddComponent<LayoutElement>();
            genBtnLayout.minHeight = 25;
            var genBtn = genBtnObj.GetComponent<Button>();
            genBtn.onClick.AddListener(() =>
            {
                if (m_currentPack == null)
                {
                    SL.LogWarning("Cannot generate a template without an inspected SLPack!");
                    return;
                }

                var newTemplate = (ContentTemplate)Activator.CreateInstance(this.m_currentGeneratorType);
                if (newTemplate.CanParseContent)
                {
                    var content = newTemplate.GetContentFromID(m_generatorTargetInput.text);

                    if (content != null && newTemplate.ParseToTemplate(content) is ContentTemplate parsed)
                    {
                        newTemplate = parsed;
                    }
                    else if (!newTemplate.DoesTargetExist)
                    {
                        SL.LogWarning("Could not find any content from target ID '" + m_generatorTargetInput.text + "'");
                        newTemplate = null;
                    }
                }

                if (newTemplate != null)
                {
                    if (!CheckTemplateName(newTemplate, subfolderInput, nameInput))
                    {
                        SL.LogWarning("A folder/filename already exists with that name, try again");
                        return;
                    }

                    // EXPORT ICONS/TEXTURES IF ITEM/STATUS/IMBUE

                    if (exportIconsWanted || exportTexturesWanted)
                    {
                        if (newTemplate is SL_Item slitem)
                        {
                            if (string.IsNullOrEmpty(slitem.SerializedSubfolderName))
                            {
                                SL.LogWarning("You need to set a subfolder name to export icons/textures!");
                                return;
                            }

                            var item = ResourcesPrefabManager.Instance.GetItemPrefab(slitem.Target_ItemID);
                            var dir = Path.Combine(m_currentPack.GetPathForCategory<ItemCategory>(), slitem.SerializedSubfolderName, "Textures");

                            if (exportIconsWanted)
                                SL_Item.SaveItemIcons(item, dir);

                            if (exportTexturesWanted)
                                SL_Item.SaveItemTextures(item, dir, out slitem.m_serializedMaterials);
                        }
                        else if (exportIconsWanted && newTemplate is SL_StatusBase slstatus)
                        {
                            var template = slstatus as ContentTemplate;

                            if (string.IsNullOrEmpty(template.SerializedSubfolderName))
                            {
                                SL.LogWarning("You need to set a subfolder name to export icons!");
                                return;
                            }

                            var dir = Path.Combine(m_currentPack.GetPathForCategory<StatusCategory>(), template.SerializedSubfolderName);

                            Component comp;
                            if (template is SL_StatusEffect sl_Status)
                                comp = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(sl_Status.TargetStatusIdentifier);
                            else
                                comp = ResourcesPrefabManager.Instance.GetEffectPreset((template as SL_ImbueEffect).TargetStatusID) as ImbueEffectPreset;

                            slstatus.ExportIcons(comp, dir);
                        }
                    }

                    var inspector = InspectorManager.Instance.Inspect(newTemplate, m_currentPack);

                    (inspector as TemplateInspector).Save();
                }
            });

            var genBtnText = genBtnObj.GetComponentInChildren<Text>();
            genBtnText.text = "Create Template";
        }

        internal bool CheckTemplateName(ContentTemplate template, InputField subfolderInput, InputField nameInput)
        {
            template.SerializedSLPackName = m_currentPack.Name;

            var subname = "";
            if (template.TemplateAllowedInSubfolder)
            {
                if (!string.IsNullOrEmpty(subfolderInput.text))
                    subname = subfolderInput.text;
                else
                    subname = Serializer.ReplaceInvalidChars(template.DefaultTemplateName);
            }

            var dir = m_currentPack.GetPathForCategory(m_currentCategory.GetType());
            if (!string.IsNullOrEmpty(subname))
            {
                var tempdir = Path.Combine(dir, subname);

                if (Directory.Exists(tempdir))
                {
                    if (!string.IsNullOrEmpty(subfolderInput.text))
                        // subfolder supplied but one exists there. return false.
                        return false;

                    int tried = 2;
                    var tempsubname = subname + "_" + tried;
                    tempdir = Path.Combine(dir, tempsubname);
                    while (Directory.Exists(tempdir))
                    {
                        tried++;
                        tempsubname = subname + "_" + tried;
                        tempdir = Path.Combine(dir, tempsubname);
                    }
                    subname = tempsubname;
                }

                dir = tempdir;
            }

            template.SerializedSubfolderName = subname;

            string name;
            if (!string.IsNullOrEmpty(nameInput.text))
                name = nameInput.text;
            else
                name = Serializer.ReplaceInvalidChars(template.DefaultTemplateName);

            var fullpath = Path.Combine(dir, $"{name}.xml");

            if (File.Exists(fullpath))
            {
                if (!string.IsNullOrEmpty(nameInput.text))
                    // name supplied but one exists there. return false.
                    return false;

                // Force auto unique name (no input supplied)
                int tried = 2;
                var test = $"{name}_{tried}";
                while (File.Exists(Path.Combine(dir, $"{test}.xml")))
                {
                    tried++;
                    test = $"{name}_{tried}";
                }
                name = test;
            }

            template.SerializedFilename = name;

            return true;
        }

        internal void AddSLPackTemplateButton(ContentTemplate template)
        {
            var rowObj = UIFactory.CreateHorizontalGroup(m_pageContent, new Color(0.15f, 0.15f, 0.15f));
            var rowLayout = rowObj.AddComponent<LayoutElement>();
            rowLayout.minHeight = 25;
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childForceExpandWidth = false;
            rowGroup.childForceExpandHeight = true;
            rowGroup.spacing = 5;
            rowGroup.padding = new RectOffset(2, 2, 2, 2);

            var labelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            var labelText = labelObj.GetComponent<Text>();
            labelText.text = $"{template.SerializedFilename} ({UISyntaxHighlight.ParseFullSyntax(template.GetType(), false)})";
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 9999;

            var inspectBtnObj = UIFactory.CreateButton(rowObj, new Color(0.1f, 0.3f, 0.1f));
            var inspectLayout = inspectBtnObj.AddComponent<LayoutElement>();
            inspectLayout.minWidth = 60;
            inspectLayout.flexibleWidth = 0;
            var inspectText = inspectBtnObj.GetComponentInChildren<Text>();
            inspectText.text = "Edit";
            var inspectBtn = inspectBtnObj.GetComponent<Button>();

            var refTemplate = template;

            inspectBtn.onClick.AddListener(() =>
            {
                if (m_currentPack == null)
                {
                    SL.LogWarning("Trying to inspect an SL Pack template, but current pack is null!");
                    return;
                }

                InspectorManager.Instance.Inspect(refTemplate, m_currentPack);
            });
        }

        #endregion
    }
}
