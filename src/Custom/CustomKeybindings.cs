using HarmonyLib;
using Rewired;
using Rewired.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RewiredDict = System.Collections.Generic.Dictionary<int, RewiredInputs>;

// Credits:
// - Stian for the original version
// - Lasyan3 for helping port it over to BepInEx
// - johnathan-clause for logic fixes and improvements (https://github.com/johnathan-clause)

namespace SideLoader
{
    public enum KeybindingsCategory
    {
        CustomKeybindings = 0,
        Menus = 1,
        QuickSlot = 2,
        Actions = 4,
    }

    public enum InputType
    {
        Axis = 0,
        Button = 1
    }

    public enum ControlType
    {
        Keyboard,
        Gamepad,
        Both
    }

    public class KeybindInfo
    {
        public string name;
        public KeybindingsCategory category;
        public ControlType controllerType;
        public InputType type;

        internal int actionID;
        internal int[] keyIDs;

        public KeybindInfo(string name, KeybindingsCategory category, ControlType controllerType, InputType type)
        {
            this.name = name;
            this.category = category;
            this.controllerType = controllerType;
            this.type = type;

            this.keyIDs = new int[2];
            this.actionID = -1;
        }

        // Used by the public CustomKeybindings.GetKey(/Down) methods.
        internal bool GetKeyDown(out int playerID) => DoGetKey(true, out playerID);
        internal bool GetKey(out int playerID) => DoGetKey(false, out playerID);

        // Actual check if the keybind is down.
        internal bool DoGetKey(bool down, out int playerID)
        {
            playerID = -1;

            // check all players in case its split screen (this is the playerID returned)
            foreach (var entry in CustomKeybindings.PlayerInputManager)
            {
                //if (entry.Value.GetButtonDown(this.name))
                //    return true;

                var id = entry.Key;
                var player = entry.Value;

                // get the last controller used for this keybinding
                var glyphData = player.GetLastUsedControllerFirstElementMapWithAction(name);
                if (glyphData.aem != null)
                {
                    // update the internal ID used for the input
                    keyIDs[id] = glyphData.aem.elementIdentifierId;

                    // get the internal controller map
                    var ctrlrList = ReInput.players.GetPlayer(id).controllers;
                    var map = glyphData.aem.controllerMap;
                    var controller = ctrlrList.GetController(map.controllerType, map.controllerId);

                    // finally we just call this to check
                    if (down)
                    {
                        if (controller.GetButtonDownById(keyIDs[id]))
                        {
                            playerID = id;
                            return true;
                        }
                    }
                    else
                    {
                        if (controller.GetButtonById(keyIDs[id]))
                        {
                            playerID = id;
                            return true;
                        }
                    }
                }
                else
                {
                    if (down)
                        return player.GetButtonDown(this.name);
                    else
                        return player.GetButton(this.name);
                }
            }

            return false;
        }
    }

    public class CustomKeybindings
    {
        internal const int CUSTOM_CATEGORY = 0;

        public static RewiredDict PlayerInputManager => ControlsInput.m_playerInputManager;

        internal static Dictionary<string, KeybindInfo> s_customKeyDict = new Dictionary<string, KeybindInfo>();
        internal static readonly HashSet<string> s_loggedMissingKeyNames = new HashSet<string>();

        /// <summary>Use this to add a new Keybinding to the game.</summary>
        /// <param name="name">The name for the keybinding displayed in the menu.</param>
        /// <param name="category">The category to add to</param>
        /// <param name="controlType">What type of control this is</param>
        /// <param name="type">What type(s) of input it will accept</param>
        public static void AddAction(string name, KeybindingsCategory category, ControlType controlType = ControlType.Keyboard, 
            InputType type = InputType.Button)
        {
            if (ReInput.initialized)
            {
                SL.LogWarning("Tried to add Custom Keybinding too late. Add your keybinding earlier, such as in your BaseUnityPlugin.Awake() method.");
                return;
            }

            if (s_customKeyDict.ContainsKey(name))
            {
                SL.LogWarning($"Attempting to add a keybind '{name}', but one with this name has already been registered.");
                return;
            }

            var customKey = new KeybindInfo(name, category, controlType, type);
            s_customKeyDict.Add(name, customKey);
        }

        /// <summary>Use this to check if a key is pressed this frame.</summary>
        /// <param name="keyName">The name of the key which you registered with.</param>
        /// <returns>True if pressed, false if not.</returns>
        public static bool GetKeyDown(string keyName) => GetKeyDown(keyName, out _);

        /// <summary>Use this to check if a key is pressed this frame, and get the local ID of the player who pressed it.</summary>
        /// <param name="keyName">The name of the key which you registered with.</param>
        /// <param name="playerID">If the key is pressed, this is the local split-player ID that pressed it.</param>
        /// <returns>True if pressed, false if not.</returns>
        public static bool GetKeyDown(string keyName, out int playerID)
        {
            if (s_customKeyDict.TryGetValue(keyName, out KeybindInfo key))
            {
                return key.GetKeyDown(out playerID);
            }

            if (!s_loggedMissingKeyNames.Contains(keyName))
            {
                SL.LogWarning($"Attempting to get custom keybinding state, but no custom keybinding " +
                    $"with the name '{keyName}' was registered, this will not be logged again.");

                s_loggedMissingKeyNames.Add(keyName);
            }

            playerID = -1;
            return false;
        }

        /// <summary>Use this to check if a key is held this frame.</summary>
        /// <param name="keyName">The name of the key which you registered with.</param>
        /// <returns>True if pressed, false if not.</returns>
        public static bool GetKey(string keyName) => GetKey(keyName, out _);

        /// <summary>Use this to check if a key is held this frame, and get the local ID of the player who pressed it.</summary>
        /// <param name="keyName">The name of the key which you registered with.</param>
        /// <param name="playerID">If the key is pressed, this is the local split-player ID that pressed it.</param>
        /// <returns>True if pressed, false if not.</returns>
        public static bool GetKey(string keyName, out int playerID)
        {
            if (s_customKeyDict.TryGetValue(keyName, out KeybindInfo key))
            {
                return key.GetKey(out playerID);
            }

            if (!s_loggedMissingKeyNames.Contains(keyName))
            {
                SL.LogWarning($"Attempting to get custom keybinding state, but no custom keybinding " +
                    $"with the name '{keyName}' was registered, this will not be logged again.");

                s_loggedMissingKeyNames.Add(keyName);
            }

            playerID = -1;
            return false;
        }

        // ===================== INTERNAL ===================== //

        // This patch is just used to grab a reference to the UserData

        //internal static UserData s_userData;

        [HarmonyPatch(typeof(InputManager_Base), "Initialize")]
        public class InputManager_Base_Initialize
        {
            [HarmonyPrefix]
            public static void InitializePatch(InputManager_Base __instance)
            {
                //s_userData = __instance.userData;

                foreach (var keybindInfo in s_customKeyDict.Values)
                {
                    // This actually creates the new actions:
                    AddRewiredAction(__instance.userData, keybindInfo.name, keybindInfo.category, keybindInfo.type, out int actionID);
                    keybindInfo.actionID = actionID;

                    SL.Log($"Set up custom keybinding '{keybindInfo.name}', actionID: " + keybindInfo.actionID);
                }
            }
        }

        // Actually add the Rewired control map

        internal static InputAction AddRewiredAction(UserData userData, string name, KeybindingsCategory category, InputType type, out int actionID)
        {
            int[] preAddIDs = userData.GetActionIds();

            // Add an action to the data store
            userData.AddAction((int)category);

            actionID = userData.GetActionIds().Where(it => !preAddIDs.Contains(it)).FirstOrDefault();

            // Get a reference to the added action
            var inputAction = userData.GetActionById(actionID);

            inputAction.name = name;
            inputAction.descriptiveName = name;
            inputAction.type = (InputActionType)type;
            inputAction.userAssignable = true;
            inputAction.categoryId = (int)category;

            return inputAction;
        }

        // Main patch where our internal setup is done

        [HarmonyPatch(typeof(ControlMappingPanel), "InitMappings")]
        public class ControlMappingPanel_InitMappings
        {
            [HarmonyPrefix]
            public static void InitMappings(ControlMappingPanel __instance, ControlMappingSection ___m_sectionTemplate,
                DictionaryExt<int, ControlMappingSection> ___m_sectionList, Controller ___m_lastJoystickController)
            {
                // filter our custom keybindings for this context
                var filter = s_customKeyDict.Values
                    .Where(it => it.category == CUSTOM_CATEGORY
                              && (it.controllerType == ControlType.Both || (int)it.controllerType == (int)__instance.ControllerType));

                // check if any keybindings in "custom" category were defined
                if (!filter.Any())
                    return;

                // set up our custom localization (if not already)
                InitLocalization();

                // set the template active while we clone from it
                ___m_sectionTemplate.gameObject.SetActive(true);
                // Create the UI (copied from how game does it)
                var mapSection = UnityEngine.Object.Instantiate(___m_sectionTemplate, ___m_sectionTemplate.transform.parent);
                // set the template inactive again
                ___m_sectionTemplate.gameObject.SetActive(false);

                // move our custom section to the top
                mapSection.transform.SetAsFirstSibling();

                // Setup our custom map section
                ___m_sectionList.Add(CUSTOM_CATEGORY, mapSection);
                mapSection.ControllerType = __instance.ControllerType;
                mapSection.SetCategory(CUSTOM_CATEGORY);

                // Init the custom keys
                if (__instance.ControllerType == ControlMappingPanel.ControlType.Keyboard)
                {
                    var kbMap = ReInput.mapping.GetKeyboardMapInstance(CUSTOM_CATEGORY, 0);
                    InitCustomSection(mapSection, kbMap, filter);

                    var mouseMap = ReInput.mapping.GetMouseMapInstance(CUSTOM_CATEGORY, 0);
                    InitCustomSection(mapSection, mouseMap, filter);
                }
                else
                {
                    var joyMap = ReInput.mapping.GetJoystickMapInstance(___m_lastJoystickController as Joystick, CUSTOM_CATEGORY, 0);
                    InitCustomSection(mapSection, joyMap, filter);
                }
            }
        }

        //Set up a control mapping UI section and bind references

        internal static void InitCustomSection(ControlMappingSection mapSection, ControllerMap _controllerMap, IEnumerable<KeybindInfo> keysToAdd)
        {
            if (_controllerMap == null)
                return;

            // Loop through the custom actions we defined
            foreach (var customKey in keysToAdd)
            {
                // add the actual keybind mapping to this controller in Rewired
                _controllerMap.CreateElementMap(customKey.actionID, Pole.Positive, KeyCode.None, ModifierKeyFlags.None);

                //var alreadyKnown = ;

                // see if the UI has already been set up for this keybind
                if (mapSection.m_actionAlreadyKnown.Contains(customKey.actionID))
                    continue;

                // set up the UI for this keybind (same as how game does it)
                mapSection.m_actionAlreadyKnown.Add(customKey.actionID);

                var action = ReInput.mapping.GetAction(customKey.actionID);

                mapSection.m_actionTemplate.gameObject.SetActive(true);
                if (action.type == InputActionType.Button)
                    mapSection.CreateActionRow(CUSTOM_CATEGORY, action, AxisRange.Positive);
                else
                {
                    if (mapSection.ControllerType == ControlMappingPanel.ControlType.Keyboard)
                    {
                        mapSection.CreateActionRow(CUSTOM_CATEGORY, action, AxisRange.Positive);
                        mapSection.CreateActionRow(CUSTOM_CATEGORY, action, AxisRange.Negative);
                    }
                    else
                        mapSection.CreateActionRow(CUSTOM_CATEGORY, action, AxisRange.Full);
                }
                mapSection.m_actionTemplate.gameObject.SetActive(false);

                // Continue the loop of custom keys...
            }
        }

        [HarmonyPatch(typeof(ControlMappingPanel), "InitSections")]
        public class ControlMappingPanel_InitSections
        {
            [HarmonyPrefix]
            public static bool InitSections(ControllerMap _controllerMap)
            {
                InitLocalization();

                if (_controllerMap.categoryId == CUSTOM_CATEGORY)
                    return false;

                foreach (var keybindInfo in s_customKeyDict.Values)
                {
                    // If the controller map's control type does not match our action
                    if (!(keybindInfo.controllerType == ControlType.Both
                        || keybindInfo.controllerType == ControlType.Keyboard && (_controllerMap is KeyboardMap || _controllerMap is MouseMap)
                        || keybindInfo.controllerType == ControlType.Gamepad && (_controllerMap is JoystickMap)))
                    {
                        // Then skip to next action
                        continue;
                    }

                    // i dont know but this gets the best results
                    if (_controllerMap.categoryId != 5)
                    {
                        // Skip to next action
                        continue;
                    }

                    //SL.LogWarning("Creating element map for '" + keybindInfo.name + "', id: " + _controllerMap.id + ", categoryId: " + _controllerMap.categoryId);

                    _controllerMap.CreateElementMap(keybindInfo.actionID, Pole.Positive, KeyCode.None, ModifierKeyFlags.None);
                }

                // We're done here. Call original implementation
                return true;
            }
        }

        internal static bool s_doneLocalization;

        public static void InitLocalization()
        {
            if (s_doneLocalization)
                return;

            s_doneLocalization = true;

            // get the Rewired Category for the category we're using
            var category = ReInput.mapping.GetActionCategory(CUSTOM_CATEGORY);
            // override the internal name
            category.name = "CustomKeybindings";

            // Get reference to localization dict
            var genLoc = References.GENERAL_LOCALIZATION;

            // add localization for our category, in the format the game will expect to find it
            genLoc.Add("ActionCategory_CustomKeybindings", "Custom Keybindings");

            // Localize the keys
            foreach (var customKey in s_customKeyDict.Values)
            {
                if (customKey.name.Contains("QS_Instant"))
                {
                    //SL.Log("Skipping localization for " + customKey.name);
                    continue;
                }

                string key = "InputAction_" + customKey.name;

                if (!genLoc.ContainsKey(key))
                    genLoc.Add(key, customKey.name);
                else
                    genLoc[key] = customKey.name;
            }
        }

        // These two patches are to fix an issue with saving our custom keybindings.
        // The map category we use is apparently not marked as UserAssignable.

        [HarmonyPatch(typeof(Player), nameof(Player.GetSaveData))]
        public class Player_GetSaveData
        {
            [HarmonyPrefix]
            public static void Prefix(ref bool userAssignableMapsOnly)
            {
                userAssignableMapsOnly = false;
            }
        }

        [HarmonyPatch(typeof(RewiredInputs), "GetAllControllerMapsXml")]
        public class RewiredInputs_GetAllControllerMapsXml
        {
            [HarmonyPrefix]
            public static void Prefix(ref bool _userAssignableMapsOnly)
            {
                _userAssignableMapsOnly = false;
            }
        }
    }
}
