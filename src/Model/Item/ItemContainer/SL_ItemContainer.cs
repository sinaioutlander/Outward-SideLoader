using System.Collections.Generic;

namespace SideLoader
{
    public class SL_ItemContainer : SL_Item
    {
        public float? BaseContainerCapacity;

        public ItemContainer.SpecialContainerTypes? SpecialType;

        public GlobalAudioManager.Sounds? OpenSound;

        public bool? AllowOverCapacity;
        public bool? CanContainMoney;
        public bool? CanReceiveCharacterItems;
        public bool? CanRemoveItems;
        public bool? NonSavableContent;
        public bool? ShowMoneyIfEmpty;
        public bool? VisibleIfEmpty;

        public SL_ItemFilter ItemFilter;

        public override void ApplyToItem(Item item)
        {
            base.ApplyToItem(item);

            var comp = item as ItemContainer;

            if (this.BaseContainerCapacity != null)
                comp.m_baseContainerCapacity = (float)this.BaseContainerCapacity;

            if (this.SpecialType != null)
                comp.SpecialType = (ItemContainer.SpecialContainerTypes)this.SpecialType;

            if (this.OpenSound != null)
                comp.OpenSoundOverride = (GlobalAudioManager.Sounds)this.OpenSound;

            if (this.AllowOverCapacity != null)
                comp.AllowOverCapacity = (bool)this.AllowOverCapacity;

            if (this.CanContainMoney != null)
                comp.CanContainMoney = (bool)this.CanContainMoney;

            if (this.CanReceiveCharacterItems != null)
                comp.CanReceiveCharacterItems = (bool)this.CanReceiveCharacterItems;

            if (this.CanRemoveItems != null)
                comp.m_canRemoveItems = (bool)this.CanRemoveItems;

            if (this.NonSavableContent != null)
                comp.NonSavableContent = (bool)this.NonSavableContent;

            if (this.ShowMoneyIfEmpty != null)
                comp.ShowMoneyIfEmpty = (bool)this.ShowMoneyIfEmpty;

            if (this.VisibleIfEmpty != null)
                comp.VisibleIfEmpty = (bool)this.VisibleIfEmpty;

            if (this.ItemFilter != null)
            {
                ItemFilter filterComp = comp.gameObject.GetOrAddComponent<ItemFilter>();
                ItemFilter.ApplyToItemFilter(filterComp);
            }
        }

        public override void SerializeItem(Item item)
        {
            base.SerializeItem(item);

            var comp = item as ItemContainer;

            this.BaseContainerCapacity = comp.m_baseContainerCapacity;
            this.SpecialType = comp.SpecialType;
            this.OpenSound = comp.OpenSoundOverride;
            this.AllowOverCapacity = comp.AllowOverCapacity;
            this.CanContainMoney = comp.CanContainMoney;
            this.CanReceiveCharacterItems = comp.CanReceiveCharacterItems;
            this.CanRemoveItems = comp.CanRemoveItems;
            this.NonSavableContent = comp.NonSavableContent;
            this.ShowMoneyIfEmpty = comp.ShowMoneyIfEmpty;
            this.VisibleIfEmpty = comp.VisibleIfEmpty;
            this.ItemFilter = SL_ItemFilter.ParseItemFilter(comp.GetComponent<ItemFilter>());
        }
    }

    public class SL_ItemFilter
    {
        public bool MatchOnEmpty;
        public List<EquipmentSlot.EquipmentSlotIDs> EquipmentTypes;
        public List<Tag.TagTypes> TagTypes;

        public void ApplyToItemFilter(ItemFilter component)
        {
            component.MatchOnEmpty = this.MatchOnEmpty;

            component.m_equipmentTypes = this.EquipmentTypes;
            component.m_tagTypeFilters = this.TagTypes;
        }

        public static SL_ItemFilter ParseItemFilter(ItemFilter itemFilter)
        {
            if (itemFilter == null)
                return null;

            var ret = new SL_ItemFilter
            {
                MatchOnEmpty = itemFilter.MatchOnEmpty,
            };

            ret.EquipmentTypes = itemFilter.m_equipmentTypes;
            ret.TagTypes = itemFilter.m_tagTypeFilters;

            return ret;
        }
    }
}
