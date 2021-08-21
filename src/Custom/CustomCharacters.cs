using SideLoader.SaveData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SideLoader
{
    /// <summary>
    /// SideLoader's manager class for Custom Characters. Contains useful methods for the creation, mangement and destruction  of SL_Characters.
    /// </summary>
    public class CustomCharacters
    {
        // ======================== PUBLIC HELPERS ======================== //

        /// <summary>
        /// Use this to cleanup a custom character. This will send out an RPC.
        /// </summary>
        /// <param name="character">The Character to destroy.</param>
        public static void DestroyCharacterRPC(Character character)
            => SLRPCManager.Instance.DestroyCharacter(character.UID);

        /// <summary>
        /// Use this to cleanup a custom character. This will send out an RPC.
        /// </summary>
        /// <param name="UID">The UID of the Character to destroy.</param>
        public static void DestroyCharacterRPC(string UID)
            => SLRPCManager.Instance.DestroyCharacter(UID);

        // ============ Spawning ============

        /// <summary>
        /// Spawns a custom character and applies the template.
        /// The OnSpawn callback is based on the Template UID. You should have already called template.Prepare() before calling this.
        /// </summary>
        /// <param name="template">The SL_Character template containing most of the main information</param>
        /// <param name="position">Optional manual spawn position, otherwise just use the template.SpawnPosition</param>
        /// <param name="characterUID">Optional manual character UID, if dynamically spawning multiple from one template.</param>
        /// <param name="extraRpcData">Optional extra RPC data to send with the spawn.</param>
        /// <returns>The GameObject of your new character</returns>
        public static GameObject SpawnCharacter(SL_Character template, Vector3 position, string characterUID = null, string extraRpcData = null)
            => SpawnCharacter(template, position, Vector3.zero, characterUID, extraRpcData);

        /// <summary>
        /// Spawns a custom character and applies the template.
        /// The OnSpawn callback is based on the Template UID. You should have already called template.Prepare() before calling this.
        /// </summary>
        /// <param name="template">The SL_Character template containing most of the main information</param>
        /// <param name="position">Spawn position for the character</param>
        /// <param name="rotation">Rotation for the character</param>
        /// <param name="characterUID">Optional manual character UID, if dynamically spawning multiple from one template.</param>
        /// <param name="extraRpcData">Optional extra RPC data to send with the spawn.</param>
        /// <returns>Your custom character</returns>
        public static GameObject SpawnCharacter(SL_Character template, Vector3 position, Vector3 rotation, string characterUID = null, string extraRpcData = null)
            => SpawnCharacter(template, position, rotation, characterUID, extraRpcData, false);

        internal static GameObject SpawnCharacter(SL_Character template, Vector3 position, Vector3 rotation, string characterUID = null, string extraRpcData = null, bool loadingFromSave = false)
           => InternalSpawn(position, rotation, characterUID ?? template.UID, template.Name, template.CharacterVisualsData?.ToString(), template.UID, extraRpcData, loadingFromSave);

        // ==================== Internal ====================

        /// <summary>Key: Spawn callback UID (generally template UID), Value: SL_Character with OnSpawn event to invoke</summary>
        public static readonly Dictionary<string, SL_Character> Templates = new Dictionary<string, SL_Character>();

        internal static readonly List<CustomSpawnInfo> ActiveCharacters = new List<CustomSpawnInfo>();

        internal static void InvokeSpawnCharacters()
        {
            if (PhotonNetwork.isNonMasterClientInRoom)
                return;

            var scene = SceneManager.GetActiveScene();
            SL.Log($"Checking SL_Character spawns ({scene.name})");

            var sceneData = SLCharacterSaveManager.TryLoadSaveData(CharSaveType.Scene);

            foreach (var template in Templates.Values.Where(it => it.SaveType == CharSaveType.Scene
                                                               && it.SceneToSpawn == SceneManagerHelper.ActiveSceneName))
            {
                if (!SLCharacterSaveManager.SceneResetWanted && sceneData != null && sceneData.Any(it => it.TemplateUID == template.UID))
                {
                    continue;
                }

                template.SceneSpawnIfValid();
            }

            SLPlugin.Instance.StartCoroutine(DelayedLoadCoroutine(sceneData));
        }

        private static IEnumerator DelayedLoadCoroutine(SL_CharacterSaveData[] sceneData)
        {
            while (!NetworkLevelLoader.Instance.AllPlayerReadyToContinue || NetworkLevelLoader.Instance.IsGameplayPaused)
                yield return null;

            if (!SLCharacterSaveManager.SceneResetWanted && sceneData != null)
                LoadCharactersFromSave(sceneData);

            if (SLCharacterSaveManager.TryLoadSaveData(CharSaveType.Follower) is SL_CharacterSaveData[] followData)
                LoadCharactersFromSave(followData);

            SLCharacterSaveManager.SceneResetWanted = false;
        }

        private static void LoadCharactersFromSave(SL_CharacterSaveData[] list)
        {
            // var playerPos = CharacterManager.Instance.GetFirstLocalCharacter().transform.position;

            foreach (var saveData in list)
            {
                if (!Templates.TryGetValue(saveData.TemplateUID, out SL_Character template))
                {
                    SL.LogWarning($"Loading an SL_Character save data, but cannot find any loaded SL_Character template with the UID '{saveData.TemplateUID}'");
                    continue;
                }

                Vector3 pos;
                if (!string.IsNullOrEmpty(saveData.FollowTargetUID)
                    && CharacterManager.Instance.GetCharacter(saveData.FollowTargetUID) is Character followTarget)
                {
                    pos = followTarget.transform.position;
                }
                else
                    pos = saveData.Position;

                if (!template.GetShouldSpawn())
                    continue;

                var character = template.InternalSpawn(pos, template.SpawnRotation, saveData.CharacterUID, saveData.ExtraRPCData, true);

                saveData.ApplyToCharacter(character);
            }
        }

        internal static void AddActiveCharacter(CustomSpawnInfo info)
        {
            if (!ActiveCharacters.Contains(info))
                ActiveCharacters.Add(info);
        }

        internal static void RequestSpawnedCharacters()
        {
            SLRPCManager.Instance.photonView.RPC(nameof(SLRPCManager.RPC_RequestCharacters), PhotonTargets.MasterClient);
        }

        // ~~~~~~~~~ Main internal spawn method ~~~~~~~~~

        public const string PLAYER_PREFAB_NAME = "NewPlayerPrefab";
        public const string TROG_PREFAB_NAME = "TrogPlayerPrefab";

        internal static GameObject InternalSpawn(Vector3 position, Vector3 rotation, string UID, string name,
            string visualData, string spawnCallbackUID, string extraRpcData, bool loadingFromSave)
        {
            SL.Log($"Spawning character '{name}', _UID: {UID}, spawnCallbackUID: {spawnCallbackUID}, at position: {position}");

            try
            {
                GameObject prefab;
                Character character;

                string prefabToMake = PLAYER_PREFAB_NAME;

                if (!string.IsNullOrEmpty(visualData))
                {
                    var vData = CharacterVisualData.CreateFromNetworkData(visualData);
                    if (vData != null && vData.SkinIndex == -999)
                        prefabToMake = TROG_PREFAB_NAME;
                }


                var args = new object[]
                {
                    (int)CharacterManager.CharacterInstantiationTypes.Temporary,
                    prefabToMake,
                    UID,
                    string.Empty // dont send a creator UID, otherwise it links the current summon (used by Conjure Ghost)
                };

                prefab = PhotonNetwork.InstantiateSceneObject($"_characters/{prefabToMake}", position, Quaternion.Euler(rotation), 0, args);
                prefab.SetActive(false);

                character = prefab.GetComponent<Character>();

                character.SetUID(UID);

                var viewID = PhotonNetwork.AllocateSceneViewID();

                if (string.IsNullOrEmpty(spawnCallbackUID))
                    spawnCallbackUID = UID;

                prefab.SetActive(true);

                SLRPCManager.Instance.SpawnCharacter(UID, viewID, name, visualData, spawnCallbackUID, extraRpcData, loadingFromSave);

                return prefab;
            }
            catch (Exception e)
            {
                SL.Log("Exception spawning character: " + e.ToString());
                return null;
            }
        }

        internal static IEnumerator SpawnCharacterCoroutine(string charUID, int viewID, string name, string visualData, string spawnCallbackUID,
            string extraRpcData, bool loadingFromSave)
        {
            // get character from manager
            Character character = CharacterManager.Instance.GetCharacter(charUID);
            float start = 0f;
            while (!character && start < 5f)
            {
                yield return null;
                start += Time.deltaTime;
                character = CharacterManager.Instance.GetCharacter(charUID);
            }

            if (!character)
            {
                SL.LogWarning("SpawnCharacterCoroutine: failed to find character by UID: " + charUID);
                yield break;
            }

            // add to cache list
            AddActiveCharacter(new CustomSpawnInfo(character, spawnCallbackUID, extraRpcData));

            if (string.IsNullOrEmpty(name))
                name = "SL_Character";

            // set name
            character.name = $"{name}_{charUID}";

            // invoke OnSpawn callback
            if (Templates.ContainsKey(spawnCallbackUID))
            {
                var template = Templates[spawnCallbackUID];
                template.INTERNAL_OnSpawn(character, extraRpcData, loadingFromSave);
            }
            else
                SL.LogWarning("Could not find template based on UID: " + spawnCallbackUID);

            character.gameObject.SetActive(true);

            if (!string.IsNullOrEmpty(visualData))
                SLPlugin.Instance.StartCoroutine(SL_Character.SetVisuals(character, visualData));

            // fix Photon View component
            if (character.gameObject.GetComponent<PhotonView>() is PhotonView view)
            {
                int id = view.viewID;
                GameObject.DestroyImmediate(view);

                if (!PhotonNetwork.isNonMasterClientInRoom && id > 0)
                    PhotonNetwork.UnAllocateViewID(view.viewID);
            }

            // setup new view
            var pView = character.gameObject.AddComponent<PhotonView>();
            pView.ownerId = 0;
            pView.ownershipTransfer = OwnershipOption.Fixed;
            pView.onSerializeTransformOption = OnSerializeTransform.PositionAndRotation;
            pView.onSerializeRigidBodyOption = OnSerializeRigidBody.All;
            pView.synchronization = ViewSynchronization.Off;

            // fix photonview serialization components
            if (pView.ObservedComponents == null || pView.ObservedComponents.Count < 1)
                pView.ObservedComponents = new List<Component>() { character };

            // set view ID last.
            pView.viewID = viewID;

            // delayed force-refresh.
            float t = 0f;
            while (t < 0.5f)
            {
                yield return null;
                t += Time.deltaTime;
            }
            character.gameObject.SetActive(false);
            character.gameObject.SetActive(true);

            // remove starting silver after delay
            if (!PhotonNetwork.isNonMasterClientInRoom)
                character.Inventory.RemoveMoney(27, true);
        }

        // called from a patch on level unload
        internal static void CleanupCharacters()
        {
            if (ActiveCharacters.Count > 0 && !PhotonNetwork.isNonMasterClientInRoom)
            {
                // SL.Log("Cleaning up " + ActiveCharacters.Count + " characters.");

                for (int i = ActiveCharacters.Count - 1; i >= 0; i--)
                {
                    var info = ActiveCharacters[i];

                    if (info.ActiveCharacter)
                        DestroyCharacterRPC(info.ActiveCharacter);
                    else
                        ActiveCharacters.RemoveAt(i);
                }
            }
        }

        internal static void DestroyCharacterLocal(Character character)
        {
            if (!character)
                return;

            character.gameObject.SetActive(false);

            var query = ActiveCharacters.Where(it => it.ActiveCharacter?.UID == character.UID);
            if (query.Any())
            {
                var info = query.First();
                ActiveCharacters.Remove(info);
            }

            SLPlugin.Instance.StartCoroutine(DelayedCharacterDestroy(character));

            //if (character)
            //    SL.LogError("ERROR - Could not seem to destroy character " + character.UID);
            //else
            //    PhotonNetwork.UnAllocateViewID(view);
        }

        private static IEnumerator DelayedCharacterDestroy(Character character)
        {
            yield return new WaitForSeconds(0.1f);

            if (CharacterManager.Instance.m_characters.ContainsKey(character.UID))
                CharacterManager.Instance.m_characters.Remove(character.UID);

            var view = character.photonView;
            int viewID = view?.viewID ?? -1;

            GameObject.DestroyImmediate(character.gameObject);

            if (view)
                GameObject.DestroyImmediate(view);

            if (viewID > 0)
                PhotonNetwork.UnAllocateViewID(viewID);
        }

        #region Stats

        /// <summary>
        /// Removes PlayerCharacterStats and replaces with CharacterStats.
        /// </summary>
        public static void FixStats(Character character)
        {
            // remove PlayerCharacterStats
            if (character.GetComponent<PlayerCharacterStats>() is PlayerCharacterStats pStats)
            {
                pStats.enabled = false;
                GameObject.DestroyImmediate(pStats);
                if (character.GetComponent<PlayerCharacterStats>())
                    GameObject.Destroy(pStats);
            }
            // add new CharacterStats
            character.m_characterStats = character.gameObject.AddComponent<CharacterStats>();
            SetupBlankCharacterStats(character.m_characterStats);
        }


        /// <summary>
        /// Resets a CharacterStats to have all default stats (default for the Player).
        /// </summary>
        /// <param name="stats"></param>
        public static void SetupBlankCharacterStats(CharacterStats stats)
        {
            stats.m_damageResistance = new Stat[] { new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f) };
            stats.m_damageProtection = new Stat[] { new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f) };
            stats.m_damageTypesModifier = new Stat[] { new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f), new Stat(0f) };
            stats.m_heatRegenRate = new Stat(0f);
            stats.m_coldRegenRate = new Stat(0f);
            stats.m_heatProtection = new Stat(0f);
            stats.m_coldProtection = new Stat(0f);
            stats.m_corruptionResistance = new Stat(0f);
            stats.m_waterproof = new Stat(0f);
            stats.m_healthRegen = new Stat(0f);
            stats.m_manaRegen = new Stat(0f);
            stats.m_statusEffectsNaturalImmunity = new TagSourceSelector[0];
        }

        #endregion

        #region ENEMY CLONE TEST
        // ===================== A test I did with cloning enemies. It mostly works. =======================

        /// <summary>
        /// [BETA] Finds a GameObject with _gameObjectName and clones it into a new Character (if it contains a Character component)
        /// </summary>
        public static void CloneCharacter(string _gameObjectName)
        {
            if (GameObject.Find(_gameObjectName) is GameObject obj && obj.GetComponent<Character>() is Character c)
            {
                CloneCharacter(c);
            }
        }

        /// <summary>
        /// [BETA] Clone a character by providing the component directly
        /// </summary>
        public static void CloneCharacter(Character _targetCharacter)
        {
            try
            {
                var targetObj = _targetCharacter.gameObject;

                // prepare original for clone
                targetObj.SetActive(false);
                bool disable = _targetCharacter.DisableAfterInit;
                _targetCharacter.DisableAfterInit = false;

                // make clone
                var clone = GameObject.Instantiate(targetObj);
                clone.SetActive(false);

                // fix original
                _targetCharacter.DisableAfterInit = disable;
                targetObj.SetActive(true);

                // fix clone UIDs, etc
                var character = clone.GetComponent<Character>();
                character.m_uid = UID.Generate();
                clone.name = "[CLONE] " + character.Name + "_" + character.UID;

                // allocate a scene view ID (will need RPC if to work in multiplayer)
                clone.GetComponent<PhotonView>().viewID = PhotonNetwork.AllocateSceneViewID();

                var items = character.GetComponentsInChildren<Item>();
                for (int i = 0; i < items.Length; i++)
                {
                    var item = items[i];

                    var new_item = ItemManager.Instance.GenerateItemNetwork(item.ItemID);
                    new_item.transform.parent = item.transform.parent;

                    GameObject.DestroyImmediate(item);
                }

                //// todo same for droptable components
                //var lootable = clone.GetComponent<LootableOnDeath>();

                //var oldTables = new List<GameObject>();

                clone.SetActive(true);

                // heal reset
                character.Stats.Reset();

                clone.transform.position = CharacterManager.Instance.GetFirstLocalCharacter().transform.position;
            }
            catch (Exception e)
            {
                SL.LogError($"Error cloning enemy: {e.GetType()}, {e.Message}\r\nStack trace: {e.StackTrace}");
            }
        }
        #endregion
    }
}
