using System;

namespace SideLoader
{
    [SL_Serialized]
    public class SL_ItemStats
    {
        public int? BaseValue;
        public float? RawWeight;
        public int? MaxDurability;

        public virtual void ApplyToItem(ItemStats stats)
        {
            //set base value
            if (this.BaseValue != null)
                stats.m_baseValue = (int)this.BaseValue;

            //set raw weight
            if (this.RawWeight != null)
                stats.m_rawWeight = (float)this.RawWeight;

            //max durability
            if (this.MaxDurability != null)
            {
                stats.m_baseMaxDurability = (int)this.MaxDurability;
                stats.StartingDurability = (int)this.MaxDurability;
            }
        }

        public static SL_ItemStats ParseItemStats(ItemStats stats)
        {
            var type = Serializer.GetBestSLType(stats.GetType());

            var holder = (SL_ItemStats)Activator.CreateInstance(type);

            holder.SerializeStats(stats);

            return holder;
        }

        public virtual void SerializeStats(ItemStats stats)
        {
            BaseValue = stats.BaseValue;
            MaxDurability = stats.MaxDurability;
            RawWeight = stats.RawWeight;
        }
    }
}
