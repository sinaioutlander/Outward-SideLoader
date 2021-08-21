namespace SideLoader
{
    public class SL_AddAllStatusEffectBuildUp : SL_Effect
    {
        public float BuildUpValue;
        public bool NoDealer;
        public bool AffectController;
        public float BuildUpBonus;

        public override void ApplyToComponent<T>(T component)
        {
            var comp = component as AddAllStatusEffectBuildUp;

            comp.BuildUpValue = this.BuildUpValue;
            comp.NoDealer = this.NoDealer;
            comp.AffectController = this.AffectController;
            comp.m_buildUpBonus = this.BuildUpBonus;
        }

        public override void SerializeEffect<T>(T effect)
        {
            var comp = effect as AddAllStatusEffectBuildUp;

            BuildUpValue = comp.BuildUpValue;
            NoDealer = comp.NoDealer;
            AffectController = comp.AffectController;
            BuildUpBonus = comp.m_buildUpBonus;
        }
    }
}
