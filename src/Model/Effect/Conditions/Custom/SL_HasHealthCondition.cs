using System;

namespace SideLoader
{
    public class SL_HasHealthCondition : SL_EffectCondition, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_HasHealthCondition);
        public Type GameModel => typeof(HasHealthCondition);

        /// <summary>The value (either literal or a percentage) which the character's health must be above (or below, if inverted)</summary>
        public float Value;
        /// <summary>If true, your Value will be used as a percentage (eg 5 = 5%, 100 = 100%)</summary>
        public bool CheckPercent;
        /// <summary>If true and CheckPercent is true, it will only consider the active max health and not the total max health.</summary>
        public bool IgnoreBurntForPercent;

        public override void ApplyToComponent<T>(T component)
        {
            var comp = component as HasHealthCondition;

            comp.Value = this.Value;
            comp.CheckPercent = this.CheckPercent;
            comp.IgnoreBurntForPercent = this.IgnoreBurntForPercent;
        }

        public override void SerializeEffect<T>(T component)
        {
            var comp = component as HasHealthCondition;

            this.CheckPercent = comp.CheckPercent;
            this.Value = comp.Value;
            this.IgnoreBurntForPercent = comp.IgnoreBurntForPercent;
        }
    }

    public class HasHealthCondition : EffectCondition, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_HasHealthCondition);
        public Type GameModel => typeof(HasHealthCondition);

        /// <summary>The value (either literal or a percentage) which the character's health must be above (or below, if inverted)</summary>
        public float Value;
        /// <summary>If true, your Value will be used as a percentage (eg 5 = 5%, 100 = 100%)</summary>
        public bool CheckPercent;
        /// <summary>If true and CheckPercent is true, it will only consider the active max health and not the total max health.</summary>
        public bool IgnoreBurntForPercent;

        public override bool CheckIsValid(Character _affectedCharacter)
        {
            if (!_affectedCharacter || !_affectedCharacter.Stats)
                return false;

            var currentHP = _affectedCharacter.Stats.CurrentHealth;

            if (CheckPercent)
            {
                var max = IgnoreBurntForPercent
                            ? _affectedCharacter.Stats.ActiveMaxHealth
                            : _affectedCharacter.Stats.MaxHealth;

                var ratio = max * Value * 0.01f;

                return currentHP >= ratio;
            }
            else
                return currentHP >= Value;
        }
    }
}
