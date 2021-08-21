using HarmonyLib;
using System;
using UnityEngine;

namespace SideLoader.Patches
{
    /// <summary>
    /// This patch fixes some things for Blast and Projectile EffectSynchronizers.
    /// Due to the way we clone and setup these prefabs, we need to fix a few things here.
    /// 1) Set the ParentEffect (the ShootBlast or ShootProjectile)
    /// 2) Re-enable the prefab.
    /// </summary>
    [HarmonyPatch(typeof(Shooter), "Setup", new Type[] { typeof(TargetingSystem), typeof(Transform) })]
    public class ShootProjectile_Setup
    {
        [HarmonyPrefix]
        public static void Prefix(Shooter __instance)
        {
            if (__instance is ShootProjectile shootProjectile && shootProjectile.BaseProjectile is Projectile projectile && !projectile.gameObject.activeSelf)
            {
                projectile.m_parentEffect = __instance;
                projectile.gameObject.SetActive(true);

            }
            else if (__instance is ShootBlast shootBlast && shootBlast.BaseBlast is Blast blast && !blast.gameObject.activeSelf)
            {
                blast.m_parentEffect = __instance;
                blast.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Patch for ShootBlastHornetControl to allow them to end based on the Lifespan.
    /// </summary>
    [HarmonyPatch(typeof(ShootBlastHornetControl), "Update")]
    public class ShootBlastHornetControl_Update
    {
        [HarmonyPostfix]
        public static void Postfix(ShootBlastHornetControl __instance, bool ___m_hornetsOut)
        {
            var lifespan = __instance.BlastLifespan;

            if (!___m_hornetsOut || lifespan <= 0)
            {
                // effect is not active. return.
                return;
            }

            //var subs = (SubEffect[])At.GetField(__instance as Effect, "m_subEffects");
            if (__instance.SubEffects != null && __instance.SubEffects.Length > 0)
            {
                var blast = __instance.SubEffects[0] as Blast;

                // effect is active, but blast has been disabled. stop effect.
                if (!blast.gameObject.activeSelf)
                    __instance.Stop();
            }
        }
    }
}
