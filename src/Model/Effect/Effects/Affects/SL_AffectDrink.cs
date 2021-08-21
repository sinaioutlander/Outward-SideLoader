namespace SideLoader
{
    public class SL_AffectDrink : SL_Effect
    {
        public float AffectQuantity;

        public override void ApplyToComponent<T>(T component)
        {
            (component as AffectDrink).SetAffectDrinkQuantity(AffectQuantity);
        }

        public override void SerializeEffect<T>(T effect)
        {
            AffectQuantity = (effect as AffectNeed).m_affectQuantity;
        }
    }
}
