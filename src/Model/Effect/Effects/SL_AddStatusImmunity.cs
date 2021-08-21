namespace SideLoader
{
    public class SL_AddStatusImmunity : SL_Effect
    {
        public string ImmunityTag;

        public override void ApplyToComponent<T>(T component)
        {
            var tag = CustomTags.GetTag(ImmunityTag, false);

            if (tag == Tag.None)
            {
                SL.LogWarning($"{this.GetType().Name}: Could not find a tag with the name '{ImmunityTag}'!");
                return;
            }

            (component as AddStatusImmunity).m_statusImmunity = new TagSourceSelector(tag);
        }

        public override void SerializeEffect<T>(T effect)
        {
            ImmunityTag = (effect as AddStatusImmunity).m_statusImmunity?.Tag.TagName;
        }
    }
}
