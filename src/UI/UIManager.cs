using SideLoader.Helpers;
using SideLoader.UI.Shared;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SideLoader.UI
{
    public static class UIManager
    {
        public static GameObject CanvasRoot { get; private set; }

        internal static Font ConsoleFont { get; private set; }
        // internal static Sprite ResizeCursor { get; private set; }

        internal const string MENU_TOGGLE_KEY = "SideLoader Menu";

        public static bool ShowMenu
        {
            get => m_showMenu;
            set
            {
                if (value && !m_showMenu)
                    ForceUnlockCursor.AddUnlockSource();
                else if (!value && m_showMenu)
                    ForceUnlockCursor.RemoveUnlockSource();

                UIManager.CanvasRoot.SetActive(value);

                m_showMenu = value;
            }
        }
        private static bool m_showMenu;

        public static void Init()
        {
            LoadBundle();

            // Create core UI Canvas and Event System handler
            CreateRootCanvas();

            // Create submodules
            new MainMenu();
            //PanelDragger.LoadCursorImage();

            // Force refresh of anchors
            Canvas.ForceUpdateCanvases();

            ShowMenu = false;
        }

        public static void OnSceneChange()
        {
            //SLPackListView.Instance?.OnSceneChange();
            //PageTwo.Instance?.OnSceneChange();
        }

        public static void Update()
        {
            if (!CanvasRoot)
                return;

            if (CustomKeybindings.GetKeyDown(MENU_TOGGLE_KEY))
            {
                ShowMenu = !ShowMenu;
            }

            MainMenu.Instance?.Update();

            if (PanelDragger.Instance != null)
                PanelDragger.Instance.Update();

            for (int i = 0; i < SliderScrollbar.Instances.Count; i++)
            {
                var slider = SliderScrollbar.Instances[i];

                if (slider.CheckDestroyed())
                    i--;
                else
                    slider.Update();
            }

            for (int i = 0; i < InputFieldScroller.Instances.Count; i++)
            {
                var input = InputFieldScroller.Instances[i];

                if (input.sliderScroller.CheckDestroyed())
                    i--;
                else
                    input.Update();
            }
        }

        private static void LoadBundle()
        {
            var bundle = AssetBundle.LoadFromFile(Path.Combine(SL.INTERNAL_FOLDER, "slgui.bundle"));

            //BackupShader = bundle.LoadAsset<Shader>("DefaultUI");

            //ResizeCursor = bundle.LoadAsset<Sprite>("cursor");

            ConsoleFont = bundle.LoadAsset<Font>("CONSOLA");

            SL.Log("Loaded UI bundle");
        }

        private static GameObject CreateRootCanvas()
        {
            GameObject rootObj = new GameObject("ExplorerCanvas");
            UnityEngine.Object.DontDestroyOnLoad(rootObj);
            rootObj.layer = 5;

            CanvasRoot = rootObj;
            CanvasRoot.transform.position = new Vector3(0f, 0f, 1f);

            Canvas canvas = rootObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.referencePixelsPerUnit = 100;
            canvas.sortingOrder = 999;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = rootObj.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            rootObj.AddComponent<GraphicRaycaster>();

            return rootObj;
        }
    }
}
