namespace SideLoader
{
    public class SL_Bag : SL_Equipment
    {
        public float? Capacity;
        public bool? Restrict_Dodge;
        public float? InventoryProtection;

        public override void ApplyToItem(Item item)
        {
            base.ApplyToItem(item);

            var bag = item as Bag;

            if (this.Capacity != null)
            {
                // set container capacity
                var container = item.transform.Find("Content").GetComponent<ItemContainerStatic>();
                container.m_baseContainerCapacity = (float)this.Capacity;
            }

            // set restrict dodge 
            if (this.Restrict_Dodge != null)
                bag.m_restrictDodge = (bool)this.Restrict_Dodge;

            if (this.InventoryProtection != null)
                bag.m_inventoryProtection = (float)this.InventoryProtection;
        }

        public override void SerializeItem(Item item)
        {
            base.SerializeItem(item);

            var bag = item as Bag;

            Capacity = bag.BagCapacity;
            Restrict_Dodge = bag.RestrictDodge;
            InventoryProtection = bag.InventoryProtection;
        }
    }
}
