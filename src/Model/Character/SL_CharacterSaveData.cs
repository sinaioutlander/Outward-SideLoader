using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SideLoader
{
    [SL_Serialized]
    public class SL_CharacterSaveData
    {
        public CharSaveType SaveType;
        public string CharacterUID;
        public string TemplateUID;

        public string FollowTargetUID;

        public Vector3 Forward;
        public Vector3 Position;

        public bool WasDead;
        public float Health;
        public string[] StatusData;

        public int Silver;

        public string ExtraRPCData;
        public string ExtraSaveData;

        public List<CharItemSaveData> ItemSaves;

        public class CharItemSaveData
        {
            public enum EquipSaveType { Equipped, Pouch, Backpack }

            public EquipSaveType Type;
            public int ItemID;
            public int Quantity;

            public EquipmentSlot.EquipmentSlotIDs EquippedInSlot;
        }

        public void ApplyToCharacter(Character character)
        {
            if (!CustomCharacters.Templates.TryGetValue(this.TemplateUID, out SL_Character template))
            {
                SL.LogWarning($"Trying to apply an SL_CharacterSaveData to a Character, but could not get any template with the UID '{this.TemplateUID}'");
                return;
            }

            SLPlugin.Instance.StartCoroutine(ApplyCoroutine(character, template));
        }

        internal IEnumerator ApplyCoroutine(Character character, SL_Character template)
        {
            yield return new WaitForSeconds(0.5f);

            if (this.Silver > 0)
                character.Inventory.AddMoney(this.Silver);

            if (!string.IsNullOrEmpty(FollowTargetUID))
            {
                var followTarget = CharacterManager.Instance.GetCharacter(FollowTargetUID);
                var aisWander = character.GetComponentInChildren<AISWander>();
                if (followTarget && aisWander)
                {
                    aisWander.FollowTransform = followTarget.transform;
                }
                else
                    SL.LogWarning("Failed setting follow target!");
            }

            if (WasDead)
            {
                character.m_loadedDead = true;

                if (character.GetComponentInChildren<LootableOnDeath>() is LootableOnDeath loot)
                    loot.m_wasAlive = false;
            }

            if (ItemSaves != null && ItemSaves.Count > 0)
            {
                foreach (var itemSave in ItemSaves)
                {
                    switch (itemSave.Type)
                    {
                        case CharItemSaveData.EquipSaveType.Pouch:
                            var item = ItemManager.Instance.GenerateItemNetwork(itemSave.ItemID);
                            if (item)
                            {
                                item.ChangeParent(character.Inventory.Pouch.transform);
                                item.RemainingAmount = itemSave.Quantity;
                            }
                            break;

                        case CharItemSaveData.EquipSaveType.Equipped:
                            SL_Character.TryEquipItem(character, itemSave.ItemID);
                            break;

                        case CharItemSaveData.EquipSaveType.Backpack:
                            item = ItemManager.Instance.GenerateItemNetwork(itemSave.ItemID);
                            if (item && character.Inventory.EquippedBag)
                            {
                                item.ChangeParent(character.Inventory.EquippedBag.Container.transform);
                                item.RemainingAmount = itemSave.Quantity;
                            }
                            break;
                    }
                }
            }

            if (character.GetComponent<CharacterStats>() is CharacterStats stats)
                stats.SetHealth(this.Health);

            if (this.StatusData != null)
            {
                var statusMgr = character.GetComponentInChildren<StatusEffectManager>(true);
                if (statusMgr)
                {
                    foreach (var statusData in this.StatusData)
                    {
                        var data = statusData.Split('|');

                        var status = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(data[0]);
                        if (!status)
                            continue;

                        var dealer = CharacterManager.Instance.GetCharacter(data[1]);
                        var effect = statusMgr.AddStatusEffect(status, dealer);

                        var remaining = float.Parse(data[2]);
                        effect.m_remainingTime = remaining;
                        if (effect.StatusData != null)
                            effect.StatusData.m_remainingLifespan = remaining;
                    }
                }
            }

            template.INTERNAL_OnSaveApplied(character, this.ExtraRPCData, this.ExtraSaveData);
        }

        // ========== parsing from CustomSpawnInfo ===========

        internal static SL_CharacterSaveData FromSpawnInfo(CustomSpawnInfo info)
        {
            // should probably debug this if it happens
            if (info.Template == null || !info.ActiveCharacter)
            {
                SL.LogWarning("Trying to save a CustomSpawnInfo, but template or activeCharacter is null!");
                return null;
            }

            var character = info.ActiveCharacter;
            var template = info.Template;

            // capture the save data in an instance
            var data = new SL_CharacterSaveData()
            {
                SaveType = template.SaveType,
                TemplateUID = template.UID,
                ExtraSaveData = template.INTERNAL_OnPrepareSave(character),
                ExtraRPCData = info.ExtraRPCData,

                CharacterUID = character.UID,
                WasDead = character.IsDead,
                Forward = character.transform.forward,
                Position = character.transform.position,
                Silver = character.Inventory.ContainedSilver,
            };

            if (character.Inventory)
            {
                data.SetSavedItems(character);
            }

            if (character.GetComponentInChildren<AISWander>() is AISWander aiWander)
            {
                if (aiWander.FollowTransform && aiWander.FollowTransform.GetComponent<Character>() is Character followTarget)
                    data.FollowTargetUID = followTarget.UID.ToString();
            }

            try
            {
                data.Health = character.Health;

                if (character.StatusEffectMngr)
                {
                    var statuses = character.StatusEffectMngr.Statuses.ToArray().Where(it => !string.IsNullOrEmpty(it.IdentifierName));
                    data.StatusData = new string[statuses.Count()];

                    int i = 0;
                    foreach (var status in statuses)
                    {
                        var sourceChar = status.m_sourceCharacterUID;
                        data.StatusData[i] = $"{status.IdentifierName}|{sourceChar}|{status.RemainingLifespan}";
                        i++;
                    }
                }

            }
            catch { }

            return data;
        }

        private void SetSavedItems(Character character)
        {
            this.ItemSaves = new List<CharItemSaveData>();

            for (int i = 0; i < (int)EquipmentSlot.EquipmentSlotIDs.Count; i++)
            {
                var slot = (EquipmentSlot.EquipmentSlotIDs)i;

                if (character.Inventory.Equipment.GetEquippedItem(slot) is Equipment equipment)
                {
                    if (equipment is Weapon weapon
                        && weapon.TwoHanded
                        && this.ItemSaves.Any(it => it.ItemID == weapon.ItemID
                                                 && it.Type == SL_CharacterSaveData.CharItemSaveData.EquipSaveType.Equipped))
                    {
                        // 2h weapon and already saved it
                        continue;
                    }

                    this.ItemSaves.Add(new SL_CharacterSaveData.CharItemSaveData
                    {
                        ItemID = equipment.ItemID,
                        Type = SL_CharacterSaveData.CharItemSaveData.EquipSaveType.Equipped,
                        EquippedInSlot = slot,
                    });
                }
            }

            if (character.Inventory.Pouch)
            {
                foreach (var item in character.Inventory.Pouch.GetContainedItems())
                {
                    this.ItemSaves.Add(new SL_CharacterSaveData.CharItemSaveData
                    {
                        ItemID = item.ItemID,
                        Quantity = item.RemainingAmount,
                        Type = SL_CharacterSaveData.CharItemSaveData.EquipSaveType.Pouch,
                        EquippedInSlot = EquipmentSlot.EquipmentSlotIDs.Count
                    });
                }
            }

            if (character.Inventory.EquippedBag)
            {
                foreach (var item in character.Inventory.EquippedBag.Container?.GetContainedItems())
                {
                    this.ItemSaves.Add(new SL_CharacterSaveData.CharItemSaveData
                    {
                        ItemID = item.ItemID,
                        Quantity = item.RemainingAmount,
                        Type = SL_CharacterSaveData.CharItemSaveData.EquipSaveType.Backpack,
                        EquippedInSlot = EquipmentSlot.EquipmentSlotIDs.Count
                    });
                }
            }
        }
    }
}
