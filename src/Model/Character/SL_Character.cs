using SideLoader.Model;
using SideLoader.SLPacks;
using SideLoader.SLPacks.Categories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace SideLoader
{
    /// <summary>SideLoader's wrapper for Custom Characters.</summary>
    [SL_Serialized]
    public class SL_Character : ContentTemplate
    {
        #region IContentTemplate

        public override string DefaultTemplateName => "Untitled Character";
        public override ITemplateCategory PackCategory => SLPackManager.GetCategoryInstance<CharacterCategory>();
        public override bool TemplateAllowedInSubfolder => false;

        public override void ApplyActualTemplate() => Internal_Apply();

        #endregion

        /// <summary> This event will be executed locally by ALL clients via RPC. Use this for any custom local setup that you need.
        /// <list type="bullet">The character is the Character your template was applied to.</list>
        /// <list type="bullet">The string is the optional extraRpcData provided when you spawned the character.</list>
        /// </summary>
        public event Action<Character, string> OnSpawn;

        /// <summary>
        /// This delegate is invoked locally when save data is loaded and applied to a character using this template. 
        /// Use this to do any custom setup you might need there.
        /// <list type="bullet">The character is the Character your template was applied to.</list>
        /// <list type="bullet">The first string is the optional extraRpcData provided when you spawned the character.</list>
        /// <list type="bullet">The second string is the optional extra save data from your OnCharacterBeingSaved method, if used.</list>
        /// </summary>
        [XmlIgnore] public Action<Character, string, string> OnSaveApplied;

        /// <summary>
        /// Invoked when the character is being saved.
        /// <list type="bullet">The (in) character is the character being saved</list>
        /// <list type="bullet">The (out) string is your extra save data you want to keep.</list>
        /// </summary>
        [XmlIgnore] public Func<Character, string> OnCharacterBeingSaved;

        /// <summary>If you define this with a function, the return bool will be used to decide if the character should spawn or not (for scene spawns).</summary>
        [XmlIgnore] public Func<bool> ShouldSpawn;

        /// <summary>Determines how this character will be saved.</summary>
        public CharSaveType SaveType;

        /// <summary>The Unique ID for this character template.</summary>
        public string UID = "";
        /// <summary>The display name for this character.</summary>
        public string Name = "";

        /// <summary>If true, the character will be automatically destroyed when it dies.</summary>
        public bool DestroyOnDeath;

        /// <summary>The default starting pose for the Character. Interrupted by moving/combat.</summary>
        public Character.SpellCastType StartingPose = Character.SpellCastType.NONE;

        /// <summary>The Scale of the character's physical model.</summary>
        public Vector3 Scale = Vector3.one;

        /// <summary>Should the character spawn Sheathed, or not?</summary>
        public bool StartSheathed = true;

        /// <summary>For Scene-type characters, the Scene Name to spawn in (referring to scene build names).</summary>
        public string SceneToSpawn;
        /// <summary>For Scene-type characters, the Vector3 position to spawn at.</summary>
        public Vector3 SpawnPosition;
        /// <summary>For Scene-type characters, the Vector3 eulerAngles rotation to spawn with.</summary>
        public Vector3 SpawnRotation;

        /// <summary>Faction to set for the Character.</summary>
        public Character.Factions? Faction = Character.Factions.NONE;

        /// <summary>Optional, manually define the factions this character can target (if has Combat AI)</summary>
        public Character.Factions[] TargetableFactions;

        /// <summary>Visual Data to set for the character.</summary>
        public VisualData CharacterVisualsData;

        // ~~~~~~~~~~ equipment ~~~~~~~~~~
        /// <summary>Item ID for Weapon</summary>
        public int? Weapon_ID;
        /// <summary>Item ID for Shield</summary>
        public int? Shield_ID;
        /// <summary>Item ID for Helmet</summary>
        public int? Helmet_ID;
        /// <summary>Item ID for Chest Armor</summary>
        public int? Chest_ID;
        /// <summary>Item ID for Boots</summary>
        public int? Boots_ID;
        /// <summary>Item ID for Backpack</summary>
        public int? Backpack_ID;

        public List<SL_ItemQty> Backpack_Items;
        public List<SL_ItemQty> Pouch_Items;

        // ~~~ loot ~~~

        public bool LootableOnDeath;
        public bool DropPouchContents;
        public bool DropWeapons;
        public string[] DropTableUIDs;

        // ~~~~~~~~~~ stats ~~~~~~~~~~
        [XmlIgnore] private const string SL_STAT_ID = "SL_Stat";

        /// <summary>Base max Health stat, default 100.</summary>
        public float? Health = 100;
        /// <summary>Base Health regen stat, default 0.</summary>
        public float? HealthRegen = 0f;
        /// <summary>BaseImpact resist stat, default 0.</summary>
        public float? ImpactResist = 0;
        /// <summary>Base Protection stat, default 0.</summary>
        public float? Protection = 0;
        /// <summary>Base Barrier stat, default 0.</summary>
        public float? Barrier = 0;
        /// <summary>Base damage resists, default all 0.</summary>
        public float[] Damage_Resists = new float[6] { 0f, 0f, 0f, 0f, 0f, 0f };
        /// <summary>Base damage bonuses, default all 0.</summary>
        public float[] Damage_Bonus = new float[6] { 0f, 0f, 0f, 0f, 0f, 0f };

        /// <summary>List of Status or Status Family Tags this character is immune to (eg Bleeding, Poison, Burning)</summary>
        public List<string> Status_Immunity = new List<string>();

        // ~~~~~~~~~~ AI States ~~~~~~~~~~
        public SL_CharacterAI AI;

        //[Obsolete("Use SL_Character.AI instead")] [XmlIgnore] public bool AddCombatAI;
        //[Obsolete("Use SL_Character.AI instead")] [XmlIgnore] public bool? CanDodge;
        //[Obsolete("Use SL_Character.AI instead")] [XmlIgnore] public bool? CanBlock;

        [Obsolete("Use 'ApplyTemplate' instead (name change).")]
        public void Prepare()
            => ApplyTemplate();

        internal void Internal_Apply()
        {
            // add uid to CustomCharacters callback dictionary
            if (!string.IsNullOrEmpty(this.UID))
            {
                if (CustomCharacters.Templates.ContainsKey(this.UID))
                {
                    SL.LogError("Trying to register an SL_Character Template, but one is already registered with this UID: " + UID);
                    return;
                }

                CustomCharacters.Templates.Add(this.UID, this);
            }

            if (this.LootableOnDeath && this.DropTableUIDs?.Length > 0 && !this.DropPouchContents)
            {
                SL.LogWarning($"SL_Character '{UID}' has LootableOnDeath=true and DropTableUIDs set, but DropPouchContents is false!" +
                    $"You should set DropPouchContents to true in this case or change your template behaviour. Forcing DropPouchContents to true.");
                this.DropPouchContents = true;
            }

            OnPrepare();

            SL.Log("Prepared SL_Character '" + Name + "' (" + UID + ").");
        }

        internal virtual void OnPrepare() { }

        public void Unregister()
        {
            if (CustomCharacters.Templates.ContainsKey(this.UID))
                CustomCharacters.Templates.Remove(this.UID);
        }

        /// <summary>
        /// Calls CustomCharacters.SpawnCharacter with this template.
        /// </summary>
        /// <param name="position">Spawn position for character. eg, template.SpawnPosition.</param>
        /// <param name="characterUID">Optional custom character UID for dynamic spawns</param>
        /// <param name="extraRpcData">Optional extra RPC data to send.</param>
        public Character Spawn(Vector3 position, string characterUID = null, string extraRpcData = null)
            => InternalSpawn(position, Vector3.zero, characterUID, extraRpcData, false);

        /// <summary>
        /// Calls CustomCharacters.SpawnCharacter with this template.
        /// </summary>
        /// <param name="position">Spawn position for character. eg, template.SpawnPosition.</param>
        /// <param name="rotation">Rotation to spawn with, eg template.SpawnRotation</param>
        /// <param name="characterUID">Optional custom character UID for dynamic spawns</param>
        /// <param name="extraRpcData">Optional extra RPC data to send.</param>
        public Character Spawn(Vector3 position, Vector3 rotation, string characterUID = null, string extraRpcData = null)
            => InternalSpawn(position, rotation, characterUID, extraRpcData, false);

        // main actual spawn method
        internal Character InternalSpawn(Vector3 position, Vector3 rotation, string characterUID, string extraRpcData, bool loadingFromSave)
        {
            characterUID = characterUID ?? this.UID;

            if (CharacterManager.Instance.GetCharacter(characterUID) is Character existing)
            {
                SL.Log("Trying to spawn a character UID " + characterUID + " but one already exists with that UID!");
                return existing;
            }

            return CustomCharacters.SpawnCharacter(this, position, rotation, characterUID, extraRpcData, loadingFromSave).GetComponent<Character>();
        }

        // called from CustomCharacters.SpawnCharacterCoroutine

        internal void INTERNAL_OnSpawn(Character character, string extraRpcData, bool loadingFromSave)
        {
            character.gameObject.SetActive(false);

            ApplyToCharacter(character, loadingFromSave);

            SL.TryInvoke(OnSpawn, character, extraRpcData);
        }

        // invoked after a save is applied

        internal void INTERNAL_OnSaveApplied(Character character, string extraRpcData, string extraSaveData)
        {
            SL.TryInvoke(OnSaveApplied, character, extraRpcData, extraSaveData);
        }

        // invoked when character is being saved

        internal string INTERNAL_OnPrepareSave(Character character)
        {
            string ret = null;

            try
            {
                ret = OnCharacterBeingSaved?.Invoke(character);
            }
            catch (Exception e)
            {
                SL.LogWarning("Exception invoking OnCharacterBeingSaved for template '" + this.UID + "'");
                SL.LogInnerException(e);
            }

            return ret;
        }

        // helper to spawn for static scene spawns

        internal void SceneSpawnIfValid()
        {
            if (PhotonNetwork.isNonMasterClientInRoom || SceneManagerHelper.ActiveSceneName != this.SceneToSpawn)
                return;

            if (!GetShouldSpawn())
                return;

            Spawn(this.SpawnPosition, this.SpawnRotation);
        }

        internal bool GetShouldSpawn()
        {
            if (this.ShouldSpawn != null)
            {
                try
                {
                    return ShouldSpawn.Invoke();
                }
                catch (Exception ex)
                {
                    SL.LogWarning("Exception invoking ShouldSpawn callback for " + this.Name + " (" + this.UID + "), not spawning.");
                    SL.LogInnerException(ex);
                    return false;
                }
            }

            return true;
        }

        // internal death callback

        internal void OnDeath(Character character)
        {
            if (this.DestroyOnDeath)
            {
                SLPlugin.Instance.StartCoroutine(DestroyOnDeathCoroutine(character));
                return;
            }

            if (LootableOnDeath && this.DropTableUIDs != null && DropTableUIDs.Length > 0)
            {
                if (character.GetComponent<LootableOnDeath>() is LootableOnDeath lootable
                    && lootable.m_wasAlive)
                {
                    foreach (var tableUID in this.DropTableUIDs)
                    {
                        if (SL_DropTable.s_registeredTables.TryGetValue(tableUID, out SL_DropTable table))
                            table.GenerateDrops(character.Inventory.Pouch.transform);
                        else
                            SL.LogWarning($"Trying to generate drops for '{UID}', " +
                                $"but could not find any registered SL_DropTable with the UID '{tableUID}'");
                    }

                    character.Inventory.MakeLootable(this.DropWeapons, this.DropPouchContents, true, false);
                }
            }
        }

        // coroutine used for destroy-on-death

        private IEnumerator DestroyOnDeathCoroutine(Character character)
        {
            yield return new WaitForSeconds(2.0f);

            CustomCharacters.DestroyCharacterRPC(character);
        }

        // ~~~~~~~~~~~~~~~~ Tempate applying ~~~~~~~~~~~~~~~~

        public virtual void ApplyToCharacter(Character character, bool loadingFromSave)
        {
            SL.Log("Applying SL_Character template, template UID: " + this.UID + ", char UID: " + character.UID);

            character.NonSavable = true;

            // set name
            if (Name != null)
            {
                character.m_nameLocKey = "";
                character.m_name = Name;
            }

            // set scale
            character.transform.localScale = this.Scale;

            // set starting pose
            if (!PhotonNetwork.isNonMasterClientInRoom)
                SLPlugin.Instance.StartCoroutine(DelayedSetPose(character));

            // if host
            if (!PhotonNetwork.isNonMasterClientInRoom)
            {
                character.OnDeath += () => { OnDeath(character); };

                // set faction
                if (Faction != null)
                    character.ChangeFaction((Character.Factions)Faction);

                if (!loadingFromSave)
                    SetItems(character);
            }

            // AI
            if (this.AI != null)
            {
                //SL.Log("SL_Character AI is " + this.AI.GetType().FullName + ", applying...");
                this.AI.Apply(character);
            }

            // stats
            SetStats(character);

            if (this.AI is SL_CharacterAIMelee aiMelee && aiMelee.ForceNonCombat)
                this.TargetableFactions = new Character.Factions[0];

            if (this.TargetableFactions != null)
            {
                var targeting = character.GetComponent<TargetingSystem>();
                if (targeting)
                    targeting.TargetableFactions = this.TargetableFactions;
                else
                    SL.LogWarning("SL_Character: Could not get TargetingSystem component!");
            }

            // loot/death
            if (LootableOnDeath)
            {
                var lootable = character.gameObject.AddComponent<LootableOnDeath>();
                lootable.DropWeapons = this.DropWeapons;
                lootable.EnabledPouch = this.DropPouchContents;

                lootable.m_character = character;

                // todo droptables
                lootable.SkinDrops = new DropInstance[0];
                lootable.LootDrops = new DropInstance[0];
            }

            character.gameObject.SetActive(true);
        }

        private IEnumerator DelayedSetPose(Character character)
        {
            yield return new WaitForSeconds(0.8f);

            // set sheathed
            if (!this.StartSheathed)
                character.SheatheInput();

            if (StartingPose != Character.SpellCastType.NONE)
                character.CastSpell(StartingPose, character.gameObject);
        }

        private void SetItems(Character character)
        {
            Bag bag = null;

            // gear
            if (Weapon_ID != null)
                TryEquipItem(character, (int)Weapon_ID);

            if (Shield_ID != null)
                TryEquipItem(character, (int)Shield_ID);

            if (Helmet_ID != null)
                TryEquipItem(character, (int)Helmet_ID);

            if (Chest_ID != null)
                TryEquipItem(character, (int)Chest_ID);

            if (Boots_ID != null)
                TryEquipItem(character, (int)Boots_ID);

            if (Backpack_ID != null)
                bag = (Bag)TryEquipItem(character, (int)Backpack_ID);

            // pouch items / backpack
            if (this.Pouch_Items != null && this.Pouch_Items.Count > 0)
            {
                if (character.Inventory?.Pouch?.transform)
                {
                    foreach (var itemQty in this.Pouch_Items)
                    {
                        var prefab = ResourcesPrefabManager.Instance.GetItemPrefab(itemQty.ItemID);

                        character.Inventory.GenerateItem(prefab, itemQty.Quantity, false);
                    }
                }
                else
                    SL.LogWarning("Trying to add pouch items but character has no pouch!");
            }

            if (this.Backpack_Items != null && this.Backpack_Items.Count > 0)
            {
                if (bag)
                {
                    var itemContainer = bag.transform.Find("Content");

                    foreach (var itemQty in this.Backpack_Items)
                    {
                        var item = ItemManager.Instance.GenerateItemNetwork(itemQty.ItemID);
                        item.ChangeParent(itemContainer);

                        if (item.IsStackable)
                            item.RemainingAmount = itemQty.Quantity;

                        if (item is FueledContainer lantern)
                            lantern.SetLight(true);
                    }
                }
                else
                    SL.LogWarning("Trying to add backpack items but character has no backpack!");
            }
        }

        public virtual void SetStats(Character character)
        {
            if (character.GetComponent<PlayerCharacterStats>())
                CustomCharacters.FixStats(character);

            var stats = character.GetComponent<CharacterStats>();

            if (!stats)
                return;

            if (Health != null)
                stats.m_maxHealthStat.AddStack(new StatStack(SL_STAT_ID, (float)Health - 100), false);

            if (HealthRegen != null)
            {
                stats.m_healthRegen.AddStack(new StatStack(SL_STAT_ID, (float)HealthRegen), false);
            }

            if (ImpactResist != null)
                stats.m_impactResistance.AddStack(new StatStack(SL_STAT_ID, (float)ImpactResist), false);

            if (Protection != null)
                stats.m_damageProtection[0].AddStack(new StatStack(SL_STAT_ID, (float)Protection), false);

            if (this.Barrier != null)
                stats.m_barrierStat.AddStack(new StatStack(SL_STAT_ID, (float)Barrier), false);

            if (Damage_Resists != null)
            {
                for (int i = 0; i < 6; i++)
                    stats.m_damageResistance[i].AddStack(new StatStack(SL_STAT_ID, Damage_Resists[i]), false);
            }

            if (Damage_Bonus != null)
            {
                for (int i = 0; i < 6; i++)
                    stats.m_damageTypesModifier[i].AddStack(new StatStack(SL_STAT_ID, Damage_Bonus[i]), false);
            }

            // status immunity
            if (this.Status_Immunity != null)
            {
                var immunities = new List<TagSourceSelector>();
                foreach (var tagName in this.Status_Immunity)
                {
                    if (CustomItems.GetTag(tagName) is Tag tag && tag != Tag.None)
                        immunities.Add(new TagSourceSelector(tag));
                }

                stats.m_statusEffectsNaturalImmunity = immunities.ToArray();
            }
        }

        /// <summary>
        /// An EquipInstantiate helper that also works on custom items. It also checks if the character owns the item and in that case tries to equip it.
        /// </summary>
        public static Item TryEquipItem(Character character, int id)
        {
            if (id == -1)
                return null;

            if (ResourcesPrefabManager.Instance.GetItemPrefab(id) is Equipment item)
            {
                if (character.Inventory.Equipment.GetEquippedItem(item.EquipSlot) is Equipment existing)
                {
                    if (existing.ItemID == id)
                        return null;
                    else
                        existing.ChangeParent(character.Inventory.Pouch.transform);
                }

                Item itemToEquip;

                if (character.Inventory.OwnsItem(id, 1))
                    itemToEquip = character.Inventory.GetOwnedItems(id)[0];
                else
                    itemToEquip = ItemManager.Instance.GenerateItemNetwork(id);

                itemToEquip.ChangeParent(character.Inventory.Equipment.GetMatchingEquipmentSlotTransform(item.EquipSlot));

                return itemToEquip;
            }

            return null;
        }

        public static IEnumerator SetVisuals(Character character, string visualData)
        {
            var visuals = character.GetComponentInChildren<CharacterVisuals>(true);

            var newData = CharacterVisualData.CreateFromNetworkData(visualData);

            if (newData.SkinIndex == -999)
                yield break;

            float start = Time.time;
            while (!visuals && Time.time - start < 5f)
            {
                yield return new WaitForSeconds(0.5f);
                visuals = character.GetComponentInChildren<CharacterVisuals>(true);
            }

            yield return new WaitForSeconds(0.5f);

            if (visuals)
            {
                // disable default visuals
                visuals.transform.Find("HeadWhiteMaleA")?.gameObject.SetActive(false);
                visuals.m_defaultHeadVisuals?.gameObject.SetActive(false);
                visuals.transform.Find("MBody0")?.gameObject.SetActive(false);
                visuals.transform.Find("MFeet0")?.gameObject.SetActive(false);

                // failsafe for head index based on skin and gender
                ClampHeadVariation(ref newData.HeadVariationIndex, (int)newData.Gender, newData.SkinIndex);

                // set visual data
                var data = visuals.VisualData;
                data.Gender = newData.Gender;
                data.SkinIndex = newData.SkinIndex;
                data.HeadVariationIndex = newData.HeadVariationIndex;
                data.HairColorIndex = newData.HairColorIndex;
                data.HairStyleIndex = newData.HairStyleIndex;

                // set to the character too
                character.VisualData = data;

                var presets = CharacterManager.CharacterVisualsPresets;

                // get the skin material
                var mat = (data.Gender == 0) ? presets.MSkins[data.SkinIndex] : presets.FSkins[data.SkinIndex];
                visuals.m_skinMat = mat;

                // apply the visuals
                var hideface = false;
                var hidehair = false;

                if (visuals.ActiveVisualsHead)
                {
                    hideface = visuals.ActiveVisualsHead.HideFace;
                    hidehair = visuals.ActiveVisualsHead.HideHair;
                }

                //SL.Log("hideface: " + hideface);

                if (!hideface)
                    visuals.LoadCharacterCreationHead(data.SkinIndex, (int)data.Gender, data.HeadVariationIndex);

                if (!hidehair)
                    ApplyHairVisuals(visuals, data.HairStyleIndex, data.HairColorIndex);

                if (!character.Inventory.Equipment.GetEquippedItem(EquipmentSlot.EquipmentSlotIDs.Chest))
                    visuals.LoadCharacterCreationBody((int)data.Gender, data.SkinIndex);

                if (!character.Inventory.Equipment.GetEquippedItem(EquipmentSlot.EquipmentSlotIDs.Foot))
                    visuals.LoadCharacterCreationBoots((int)data.Gender, data.SkinIndex);
            }
            else
            {
                SL.Log("Couldn't get visuals!");
            }
        }

        public static void ApplyHairVisuals(CharacterVisuals visuals, int _hairStyleIndex, int _hairColorIndex)
        {
            var presets = CharacterManager.CharacterVisualsPresets;
            var key = $"Hair{_hairStyleIndex}";
            var dict = visuals.m_armorVisualPreview;

            Material material = presets.HairMaterials[_hairColorIndex];
            ArmorVisuals hairVisuals;

            if (dict.ContainsKey(key))
            {
                if (!dict[key].gameObject.activeSelf)
                {
                    dict[key].gameObject.SetActive(true);
                }

                hairVisuals = dict[key];
            }
            else
            {
                visuals.UseDefaultVisuals = true;

                hairVisuals = visuals.InstantiateVisuals(presets.Hairs[_hairStyleIndex].transform, visuals.transform).GetComponent<ArmorVisuals>();

                visuals.DefaultHairVisuals = hairVisuals;

                if (!hairVisuals.gameObject.activeSelf)
                    hairVisuals.gameObject.SetActive(true);

                // Add to dict
                dict.Add(key, hairVisuals);
            }

            if (_hairStyleIndex != 0)
            {
                if (!hairVisuals.Renderer)
                {
                    var renderer = hairVisuals.GetComponent<SkinnedMeshRenderer>();
                    hairVisuals.m_skinnedMeshRenderer = renderer;
                }

                hairVisuals.Renderer.material = material;
                visuals.FinalizeSkinnedRenderer(hairVisuals.Renderer);
            }

            hairVisuals.ApplyToCharacterVisuals(visuals);
        }

        public enum Ethnicities
        {
            White,
            Black,
            Asian
        }

        // This helper clamps the desired head variation within the available options.
        // Minimum is always 0 obviously, but the max index varies between gender and skin index.
        private static void ClampHeadVariation(ref int index, int gender, int skinindex)
        {
            if (skinindex == -999)
                return;

            // get the first letter of the gender name (either M or F)
            // Could cast to Character.Gender and then Substring(0, 1), but that seems unnecessary over a simple if/else.
            string sex = gender == 0 ? "M" : "F";

            // cast skinindex to ethnicity name
            string ethnicity = ((Ethnicities)skinindex).ToString();

            // get the field name (eg. MHeadsWhite, FHeadsBlack, etc)
            string fieldName = sex + "Heads" + ethnicity;

            var array = (GameObject[])At.GetField(CharacterManager.CharacterVisualsPresets, fieldName);

            int limit = array.Length - 1;

            // clamp desired head inside 0 and limit
            index = Mathf.Clamp(index, 0, limit);
        }

        /// <summary>Wrapper for Visual Data to apply to a Character.</summary>
        [SL_Serialized]
        public class VisualData
        {
            /// <summary>Gender of the character (Male or Female)</summary>
            public Character.Gender Gender = Character.Gender.Male;
            /// <summary>Hair color index (refer to character creation options)</summary>
            public int HairColorIndex = 0;
            /// <summary>Hair style index (refer to character creation options)</summary>
            public int HairStyleIndex = 0;
            /// <summary>Head variation index (refer to character creation options)</summary>
            public int HeadVariationIndex = 0;
            /// <summary>Skin index (refer to character creation options)</summary>
            public int SkinIndex = 0;

            /// <summary>
            /// Generates a string for this VisualData with CharacterVisualData.ToNetworkData()
            /// </summary>
            public override string ToString()
            {
                return new CharacterVisualData()
                {
                    Gender = this.Gender,
                    HairColorIndex = this.HairColorIndex,
                    HeadVariationIndex = this.HeadVariationIndex,
                    HairStyleIndex = this.HairStyleIndex,
                    SkinIndex = this.SkinIndex
                }.ToNetworkData();
            }
        }
    }
}
