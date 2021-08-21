using System;

namespace SideLoader
{
    public class SL_AffectCurrentHealth : SL_Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_AffectCurrentHealth);
        public Type GameModel => typeof(AffectCurrentHealth);

        /// <summary>
        /// The percent of the character's current health to affect. Eg, '50' would grant +50% current health. '-50' would take off 50% of current health. 
        /// 'Current health' means it does not include Burnt health.
        /// </summary>
        public float HealthPercent;

        public override void ApplyToComponent<T>(T component)
        {
            (component as AffectCurrentHealth).HealthPercent = this.HealthPercent;
        }

        public override void SerializeEffect<T>(T effect)
        {
            this.HealthPercent = (effect as AffectCurrentHealth).HealthPercent;
        }
    }

    public class AffectCurrentHealth : Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_AffectCurrentHealth);
        public Type GameModel => typeof(AffectCurrentHealth);

        public float HealthPercent;

        public override void ActivateLocally(Character _affectedCharacter, object[] _infos)
        {
            if (_affectedCharacter && _affectedCharacter.Alive)
            {
                float value = HealthPercent * _affectedCharacter.Stats.ActiveMaxHealth * 0.01f;

                _affectedCharacter.Stats.AffectHealth(value);

                if (!_affectedCharacter.Alive)
                    _affectedCharacter.CheckHealth();
            }
        }
    }
}
