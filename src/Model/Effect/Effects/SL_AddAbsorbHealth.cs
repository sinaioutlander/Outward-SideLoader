namespace SideLoader
{
    public class SL_AddAbsorbHealth : SL_Effect
    {
        public float HealthRatio;

        public override void ApplyToComponent<T>(T component)
        {
            (component as AddAbsorbHealth).m_healthRatio = this.HealthRatio;
        }

        public override void SerializeEffect<T>(T effect)
        {
            this.HealthRatio = (effect as AddAbsorbHealth).m_healthRatio;
        }
    }
}
