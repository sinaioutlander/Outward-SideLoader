using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideLoader.Patches
{
    [HarmonyPatch(typeof(Deployable), nameof(Deployable.StartDeployAnimation))]
    public class Deployable_StartDeployAnimation
    {
        [HarmonyPrefix]
        public static void Prefix(Deployable __instance)
        {
            if (!__instance.m_item)
                __instance.m_item = __instance.GetComponent<Item>();
        }
    }
}
