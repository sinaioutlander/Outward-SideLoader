namespace SideLoader
{
    public class SL_AffectFatigue : SL_Effect
    {
        public float AffectQuantity;

        public override void ApplyToComponent<T>(T component)
        {
            (component as AffectFatigue).SetAffectFatigueQuantity(AffectQuantity);
        }

        public override void SerializeEffect<T>(T effect)
        {
            AffectQuantity = (effect as AffectNeed).m_affectQuantity;
        }
    }
}
