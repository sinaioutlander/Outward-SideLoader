using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace SideLoader.Patches
{
    // ======== Merchants ========

    [HarmonyPatch(typeof(MerchantPouch), nameof(MerchantPouch.RefreshInventory))]
    public class MerchantPouch_RefreshInventory
    {
        [HarmonyPrefix]
        public static void Prefix(ref object[] __state, double ___m_nextRefreshTime)
        {
            if (PhotonNetwork.isNonMasterClientInRoom || !SL_DropTableAddition.s_registeredDropTableSources.Any())
                return;

            __state = new object[]
            {
                (bool)(EnvironmentConditions.GameTime >= ___m_nextRefreshTime)
            };
        }

        [HarmonyPostfix]
        public static void Postfix(MerchantPouch __instance, object[] __state)
        {
            if (PhotonNetwork.isNonMasterClientInRoom || !SL_DropTableAddition.s_registeredDropTableSources.Any())
                return;

            if (__state == null || !(bool)__state[0])
                return;

            var merchant = __instance.GetComponentInParent<Merchant>();

            foreach (var dropAddition in SL_DropTableAddition.s_registeredDropTableSources)
            {
                if (dropAddition.IsTargeting(merchant.HolderUID))
                    dropAddition.GenerateItems(__instance.transform);
            }
        }
    }

    // ======== Enemies ========

    [HarmonyPatch(typeof(LootableOnDeath), "OnDeath")]
    public class LootableOnDeath_OnDeath
    {
        [HarmonyPrefix]
        public static void Prefix(LootableOnDeath __instance, bool _loadedDead)
        {
            if (_loadedDead || !__instance.enabled || PhotonNetwork.isNonMasterClientInRoom || !SL_DropTableAddition.s_registeredDropTableSources.Any())
                return;

            //var m_character = At.GetField(__instance, "m_character") as Character;

            if (!__instance.m_character)
                return;

            if (!__instance.m_character.Alive || (__instance.m_character.IsDead && __instance.m_wasAlive))
            {
                if (__instance.m_lootDroppers == null || !__instance.m_lootDroppers.Any())
                    return;

                foreach (var dropable in __instance.m_lootDroppers)
                {
                    foreach (var dropAddition in SL_DropTableAddition.s_registeredDropTableSources)
                    {
                        if (dropAddition.IsTargeting(dropable.name))
                            dropAddition.GenerateItems(__instance.m_character.Inventory.Pouch.transform);
                    }
                }
            }
        }
    }

    // ======== Self-Filled Item Containers ========

    [HarmonyPatch(typeof(SelfFilledItemContainer), "GenerateContents")]
    public class SelfFilledItemContainer_Method
    {
        [HarmonyPostfix]
        public static void Postfix(SelfFilledItemContainer __instance)
        {
            if (PhotonNetwork.isNonMasterClientInRoom)
                return;

            foreach (var dropable in __instance.m_drops)
            {
                foreach (var dropAddition in SL_DropTableAddition.s_registeredDropTableSources)
                {
                    if (dropAddition.IsTargeting(dropable.name))
                        dropAddition.GenerateItems(__instance.transform);
                }
            }
        }
    }
}
