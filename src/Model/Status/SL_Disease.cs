namespace SideLoader
{
    public class SL_Disease : SL_StatusEffect
    {
        public int? AutoHealTime;
        public bool? CanDegenerate;
        public float? DegenerateTime;
        public Diseases? DiseaseType;
        public int? SleepHealTime;

        internal override void Internal_ApplyTemplate(StatusEffect status)
        {
            base.Internal_ApplyTemplate(status);

            var comp = status as Disease;

            if (this.AutoHealTime != null)
                comp.m_autoHealTime = (int)this.AutoHealTime;

            if (this.CanDegenerate != null)
                comp.m_canDegenerate = (bool)this.CanDegenerate;

            if (this.DegenerateTime != null)
                comp.m_degenerateTime = (float)this.DegenerateTime;

            if (this.DiseaseType != null)
                comp.m_diseasesType = (Diseases)this.DiseaseType;

            if (this.SleepHealTime != null)
                comp.m_straightSleepHealTime = (int)this.SleepHealTime;
        }

        public override void SerializeStatus(StatusEffect status)
        {
            base.SerializeStatus(status);

            var comp = status as Disease;

            this.AutoHealTime = comp.m_autoHealTime;
            this.CanDegenerate = comp.m_canDegenerate;
            this.DegenerateTime = comp.m_degenerateTime;
            this.DiseaseType = comp.m_diseasesType;
            this.SleepHealTime = comp.StraightSleepHealTime;
        }
    }
}
