using UnityEngine;

namespace SideLoader
{
    public class SL_Sleepable : SL_ItemExtension
    {
        public string RestStatusIdentifier;
        public bool? AffectFoodDrink;
        public int? AmbushReduction;
        public int? Capacity;
        public Vector3? CharAnimOffset;
        public int? ColdProtection;
        public int? CorruptionDamage;
        public int? HealthRecuperationModifier;
        public int? HeatProtection;
        public bool? IsInnsBed;
        public int? ManaPreservationModifier;
        public bool? Rejuvenate;
        public int? StaminaRecuperationModifier;

        public override void ApplyToComponent<T>(T component)
        {
            var comp = component as Sleepable;

            if (!string.IsNullOrEmpty(this.RestStatusIdentifier))
            {
                var status = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(this.RestStatusIdentifier);
                if (status)
                    comp.m_restEffect = status;
            }

            if (this.AffectFoodDrink != null)
                comp.AffectFoodDrink = (bool)this.AffectFoodDrink;

            if (this.AmbushReduction != null)
                comp.AmbushReduction = (int)this.AmbushReduction;

            if (this.Capacity != null)
                comp.Capacity = (int)this.Capacity;

            if (this.CharAnimOffset != null)
                comp.CharAnimOffset = (Vector3)this.CharAnimOffset;

            if (this.ColdProtection != null)
                comp.ColdProtection = (int)this.ColdProtection;

            if (this.CorruptionDamage != null)
                comp.CorruptionDamage = (int)this.CorruptionDamage;

            if (this.HealthRecuperationModifier != null)
                comp.HealthRecuperationModifier = (int)this.HealthRecuperationModifier;

            if (this.HeatProtection != null)
                comp.HeatProtection = (int)this.HeatProtection;

            if (this.IsInnsBed != null)
                comp.IsInnsBed = (bool)this.IsInnsBed;

            if (this.ManaPreservationModifier != null)
                comp.ManaPreservationModifier = (int)this.ManaPreservationModifier;

            if (this.Rejuvenate != null)
                comp.Rejuvenate = (bool)this.Rejuvenate;

            if (this.StaminaRecuperationModifier != null)
                comp.StaminaRecuperationModifier = (int)this.StaminaRecuperationModifier;
        }

        public override void SerializeComponent<T>(T extension)
        {
            var comp = extension as Sleepable;

            this.AffectFoodDrink = comp.AffectFoodDrink;
            this.AmbushReduction = comp.AmbushReduction;
            this.Capacity = comp.Capacity;
            this.CharAnimOffset = comp.CharAnimOffset;
            this.ColdProtection = comp.ColdProtection;
            this.CorruptionDamage = comp.CorruptionDamage;
            this.HealthRecuperationModifier = comp.HealthRecuperationModifier;
            this.HeatProtection = comp.HeatProtection;
            this.IsInnsBed = comp.IsInnsBed;
            this.ManaPreservationModifier = comp.ManaPreservationModifier;
            this.Rejuvenate = comp.Rejuvenate;
            this.StaminaRecuperationModifier = comp.StaminaRecuperationModifier;

            if (comp.ResetEffect)
                this.RestStatusIdentifier = comp.ResetEffect.IdentifierName;
        }
    }
}
