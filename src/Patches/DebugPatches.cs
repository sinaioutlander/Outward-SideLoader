using HarmonyLib;
using SideLoader.SLPacks.Categories;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SideLoader.Patches
{
    [HarmonyPatch(typeof(DT_ItemSpawner), nameof(DT_ItemSpawner.Show))]
    public class DT_ItemSpawner_Show
    {
        [HarmonyPrefix]
        public static void Prefix(DT_ItemSpawner __instance)
        {
            if (__instance.m_allItems != null)
            {
                var itemsToAdd = new HashSet<Item>();
                foreach (var itemToAdd in ItemCategory.AllCurrentTemplates)
                    if (itemToAdd.CurrentPrefab)
                        itemsToAdd.Add(itemToAdd.CurrentPrefab);

                for (int i = __instance.m_allItems.Count - 1; i >= 0; i--)
                {
                    var item = __instance.m_allItems[i];
                    if (!item)
                    {
                        __instance.m_allItems.RemoveAt(i);
                        continue;
                    }

                    if (itemsToAdd.Contains(item))
                        itemsToAdd.Remove(item);
                }

                if (itemsToAdd.Any())
                {
                    foreach (var item in itemsToAdd)
                        __instance.m_allItems.Add(item);
                }
            }
        }
    }

    [HarmonyPatch(typeof(DT_SkillProficiencyCheats), nameof(DT_SkillProficiencyCheats.Show))]
    public class DT_SkillProfiencyCheats_Show
    {
        [HarmonyPrefix]
        public static void Prefix(ref List<Skill> ___m_allSkills)
        {
            if (___m_allSkills == null)
                ___m_allSkills = ResourcesPrefabManager.Instance.EDITOR_GetPlayerSkillPrefabs();

            for (int i = 0; i < ___m_allSkills.Count; i++)
            {
                var entry = ___m_allSkills[i];
                if (!entry)
                {
                    ___m_allSkills.RemoveAt(i);
                    i--;
                    GameObject.Destroy(entry);
                }
            }

            foreach (var skill in SL_Skill.s_customSkills)
            {
                if (!___m_allSkills.Contains(skill.Value))
                {
                    //SL.LogWarning("Adding custom skill to F3 menu: " + skill.Value.Name);
                    ___m_allSkills.Add(skill.Value);
                }
            }
        }

        [HarmonyPostfix]
        public static void Postfix(DT_SkillProficiencyCheats __instance, List<DT_SkillDisplayCheat> ___m_skillDisplays,
            DT_SkillDisplayCheat ___m_skillDisplayTemplate, CharacterUI ___m_characterUI, List<int> ___m_knownSkillDisplay,
            List<int> ___m_unknownSkillDisplay, RectTransform ___m_knownSkillHolder, RectTransform ___m_unknownSkillHolder)
        {
            for (int i = 0; i < ___m_skillDisplays.Count; i++)
            {
                var entry = ___m_skillDisplays[i];
                if (!entry.RefSkill)
                {
                    ___m_skillDisplays.RemoveAt(i);
                    GameObject.Destroy(entry.gameObject);
                    i--;
                }
            }

            foreach (var skill in SL_Skill.s_customSkills)
            {
                if (!skill.Value)
                    continue;

                var query = ___m_skillDisplays.Where(it => it.RefSkill == skill.Value);

                if (!query.Any())
                {
                    //SL.LogWarning("Adding custom skill display to F3 menu: " + skill.Value.Name);

                    var display = UnityEngine.Object.Instantiate(___m_skillDisplayTemplate);
                    display.SetReferencedSkill(skill.Value);
                    display.SetCharacterUI(___m_characterUI);
                    ___m_skillDisplays.Add(display);

                    if (__instance.LocalCharacter.Inventory.SkillKnowledge.IsItemLearned(skill.Value.ItemID))
                    {
                        ___m_knownSkillDisplay.Add(___m_skillDisplays.IndexOf(display));
                        display.transform.SetParent(___m_knownSkillHolder);
                        display.transform.ResetLocal();
                    }
                    else
                    {
                        ___m_unknownSkillDisplay.Add(___m_skillDisplays.IndexOf(display));
                        display.transform.SetParent(___m_unknownSkillHolder);
                        display.transform.ResetLocal();
                    }

                    display.gameObject.SetActive(true);
                }
                else
                {
                    foreach (var result in query)
                        result.gameObject.SetActive(true);
                }
            }
        }
    }
}
