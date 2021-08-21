using UnityEngine;

namespace SideLoader
{
    public class SL_PistolSkill : SL_AttackSkill
    {
        public PistolSkill.LoadoutStateCondition? AlternativeActivationRequirement;
        public PistolSkill.LoadoutStateCondition? PrimaryActivationRequirement;

        public override void ApplyToItem(Item item)
        {
            base.ApplyToItem(item);

            var comp = item as PistolSkill;

            if (this.AlternativeActivationRequirement != null)
                comp.AlternateActivationLoadoutReq = (PistolSkill.LoadoutStateCondition)this.AlternativeActivationRequirement;

            if (this.PrimaryActivationRequirement != null)
                comp.PrimaryActivationLoadoutReq = (PistolSkill.LoadoutStateCondition)this.PrimaryActivationRequirement;

            if (comp.m_isBaseFireReloadSkill && comp.transform.Find("NormalReload") is Transform reload)
            {
                comp.m_alternateAnimConditionsHolder = reload.gameObject;

                foreach (var icon in comp.m_alternateIcons)
                    icon.m_conditionHolder = reload.gameObject;
            }
        }

        public override void SerializeItem(Item item)
        {
            base.SerializeItem(item);

            var comp = item as PistolSkill;

            AlternativeActivationRequirement = comp.AlternateActivationLoadoutReq;
            PrimaryActivationRequirement = comp.PrimaryActivationLoadoutReq;
        }
    }
}
