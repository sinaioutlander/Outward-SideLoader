//using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SideLoader.UI.Modules
{
    public class ToolsPage : MainMenu.Page
    {
        public override string Name => "Tools";

        public override void Init()
        {
            ConstructUI();

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }

        private void SceneManager_activeSceneChanged(Scene _, Scene next)
        {
            m_sceneInput.text = next.name;
        }

        public override void Update()
        {
            // update position
            var character = CharacterManager.Instance.GetFirstLocalCharacter();
            if (character)
                m_positionInput.text = character.transform.position.ToString("F3");
            else
                m_positionInput.text = "<color=grey><i>No character loaded...</i></color>";

            // update current sound track
            if (References.GLOBALAUDIOMANAGER)
                m_soundInput.text = GlobalAudioManager.s_currentMusic.ToString();
            else
                m_soundInput.text = "<color=grey><i>GlobalAudioManager still loading...</i></color>";
        }

        internal InputField m_positionInput;
        internal InputField m_sceneInput;
        internal InputField m_soundInput;

        internal void ConstructUI()
        {
            GameObject parent = MainMenu.Instance.PageViewport;

            Content = UIFactory.CreateVerticalGroup(parent, new Color(0.15f, 0.15f, 0.15f));
            var mainGroup = Content.GetComponent<VerticalLayoutGroup>();
            mainGroup.padding.left = 4;
            mainGroup.padding.right = 4;
            mainGroup.padding.top = 4;
            mainGroup.padding.bottom = 4;
            mainGroup.spacing = 5;
            mainGroup.childForceExpandHeight = false;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;

            // ~~~~~ Title ~~~~~

            GameObject titleObj = UIFactory.CreateLabel(Content, TextAnchor.UpperLeft);
            Text titleLabel = titleObj.GetComponent<Text>();
            titleLabel.text = "Tools";
            titleLabel.fontSize = 20;
            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.minHeight = 30;
            titleLayout.flexibleHeight = 0;

            AddReferencedInput(out this.m_positionInput, "Player Position", "The Vector3 position of the first local player.");
            AddReferencedInput(out this.m_sceneInput, "Current Scene", "The build name of the current Scene.");
            AddReferencedInput(out this.m_soundInput, "Current Music", "The name of the current music track.");

            m_sceneInput.text = SceneManager.GetActiveScene().name;
        }

        internal void AddReferencedInput(out InputField inputfield, string title, string description)
        {
            var groupObj = UIFactory.CreateVerticalGroup(Content, new Color(0.1f, 0.1f, 0.1f));
            var group = groupObj.GetComponent<VerticalLayoutGroup>();
            group.spacing = 5;
            group.padding = new RectOffset(4, 4, 4, 4);
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            var titleObj = UIFactory.CreateLabel(groupObj, TextAnchor.MiddleLeft);
            var titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.minHeight = 35;
            var titleText = titleObj.GetComponent<Text>();
            titleText.text = title;

            var descObj = UIFactory.CreateLabel(groupObj, TextAnchor.MiddleLeft);
            var descLayout = descObj.AddComponent<LayoutElement>();
            descLayout.minHeight = 24;
            var descText = descObj.GetComponent<Text>();
            descText.text = description;

            var inputFieldObj = UIFactory.CreateInputField(groupObj);
            var inputLayout = inputFieldObj.AddComponent<LayoutElement>();
            inputLayout.minHeight = 25;
            inputLayout.flexibleHeight = 0;
            inputfield = inputFieldObj.GetComponent<InputField>();
        }
    }
}
