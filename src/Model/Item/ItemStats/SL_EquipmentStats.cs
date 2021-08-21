using System;

namespace SideLoader
{
    public class SL_EquipmentStats : SL_ItemStats
    {
        public float[] Damage_Resistance = new float[9];
        public float? Impact_Resistance;
        public float? Damage_Protection;

        public float[] Damage_Bonus = new float[9];
        public float? Impact_Bonus;

        public float? Stamina_Use_Penalty;
        public float? Mana_Use_Modifier;
        public float? Mana_Regen;
        public float? Movement_Penalty;

        public float? Pouch_Bonus;
        public float? Heat_Protection;
        public float? Cold_Protection;

        // Soroboreans
        public float? Corruption_Protection;
        public float? Health_Regen;
        public float? Cooldown_Reduction;

        // TTB
        public float? BarrierProtection;
        public float? GlobalStatusEffectResistance;
        public float? StaminaRegenModifier;

        public override void ApplyToItem(ItemStats stats)
        {
            base.ApplyToItem(stats);

            var eStats = stats as EquipmentStats;

            if (this.Damage_Resistance != null)
                eStats.m_damageResistance = this.Damage_Resistance;

            if (this.Impact_Resistance != null)
                eStats.m_impactResistance = (float)this.Impact_Resistance;

            if (this.Damage_Protection != null)
                eStats.m_damageProtection = new float[9] {(float)this.Damage_Protection, 0, 0, 0, 0, 0, 0, 0, 0 };

            if (this.Damage_Bonus != null)
                eStats.m_damageAttack = this.Damage_Bonus;

            if (this.Impact_Bonus != null)
                eStats.m_baseImpactModifier = (float)this.Impact_Bonus;

            if (this.Stamina_Use_Penalty != null)
                eStats.m_staminaUsePenalty = (float)this.Stamina_Use_Penalty;

            if (this.Mana_Use_Modifier != null)
                eStats.m_manaUseModifier = (float)this.Mana_Use_Modifier;

            if (this.Mana_Regen != null)
                eStats.m_baseManaRegenBonus = (float)this.Mana_Regen;

            if (this.Movement_Penalty != null)
                eStats.m_movementPenalty = (float)this.Movement_Penalty;

            if (this.Pouch_Bonus != null)
                eStats.m_pouchCapacityBonus = (float)this.Pouch_Bonus;

            if (this.Heat_Protection != null)
                eStats.m_heatProtection = (float)this.Heat_Protection;

            if (this.Cold_Protection != null)
                eStats.m_coldProtection = (float)this.Cold_Protection;

            if (this.Corruption_Protection != null)
                eStats.m_corruptionProtection = (float)this.Corruption_Protection;

            if (this.Cooldown_Reduction != null)
                eStats.m_baseCooldownReductionBonus = (float)this.Cooldown_Reduction;

            if (this.Health_Regen != null)
                eStats.m_baseHealthRegenBonus = (float)this.Health_Regen;

            if (this.BarrierProtection != null)
                eStats.m_baseBarrierProtection = (float)this.BarrierProtection;

            if (this.GlobalStatusEffectResistance != null)
                eStats.m_baseStatusEffectResistance = (float)this.GlobalStatusEffectResistance;

            if (this.StaminaRegenModifier != null)
                eStats.m_baseStaminaRegen = (float)this.StaminaRegenModifier;
        }

        public override void SerializeStats(ItemStats stats)
        {
            base.SerializeStats(stats);

            try
            {
                var eStats = stats as EquipmentStats;

                Impact_Resistance = eStats.ImpactResistance;
                Damage_Protection = eStats.GetDamageProtection(DamageType.Types.Physical);
                Stamina_Use_Penalty = eStats.StaminaUsePenalty;
                Mana_Use_Modifier = eStats.ManaUseModifier;
                Mana_Regen = eStats.ManaRegenBonus;
                Movement_Penalty = eStats.MovementPenalty;
                Pouch_Bonus = eStats.PouchCapacityBonus;
                Heat_Protection = eStats.HeatProtection;
                Cold_Protection = eStats.ColdProtection;

                Corruption_Protection = eStats.CorruptionResistance;
                Health_Regen = eStats.HealthRegenBonus;
                Cooldown_Reduction = eStats.CooldownReduction;

                BarrierProtection = eStats.BarrierProtection;
                GlobalStatusEffectResistance = eStats.GlobalStatusEffectResistance;
                StaminaRegenModifier = eStats.StaminaRegenModifier;

                Damage_Bonus = eStats.m_damageAttack;
                Damage_Resistance = eStats.m_damageResistance;
                Impact_Bonus = eStats.ImpactModifier;
            }
            catch (Exception e)
            {
                SL.Log("Exception getting EquipmentStats of " + stats.name + "\r\n" + e.Message + "\r\n" + e.StackTrace);
            }
        }
    }
}
