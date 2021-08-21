using System.Collections.Generic;
using System.Linq;

namespace SideLoader
{
    public class SL_BuildingUpgrade : SL_BasicDeployable
    {
        public int? BuildingToUpgradeID;
        public float? SnappingRadius;
        public string[] RequiredQuestEventUIDs;
        public int? UpgradeFromIndex;
        public int? UpgradeIndex;

        public override void ApplyToComponent<T>(T component)
        {
            base.ApplyToComponent(component);

            var comp = component as BuildingUpgrade;

            if (this.BuildingToUpgradeID != null)
                comp.BuildingToUpgrade = ResourcesPrefabManager.Instance.GetItemPrefab((int)this.BuildingToUpgradeID) as Building;

            if (this.SnappingRadius != null)
                comp.m_snappingRadius = (float)this.SnappingRadius;

            if (this.UpgradeFromIndex != null)
                comp.UpgradeFromIndex = (int)this.UpgradeFromIndex;

            if (this.UpgradeIndex != null)
                comp.UpgradeIndex = (int)this.UpgradeIndex;

            if (this.RequiredQuestEventUIDs != null)
            {
                var list = new List<QuestEventReference>();
                foreach (var evt in this.RequiredQuestEventUIDs)
                    list.Add(new QuestEventReference { m_eventUID = evt });
                comp.RequiredQuestEvents = list.ToArray();
            }
        }

        public override void SerializeComponent<T>(T extension)
        {
            base.SerializeComponent(extension);

            var comp = extension as BuildingUpgrade;

            this.BuildingToUpgradeID = comp.BuildingToUpgrade?.ItemID;
            this.SnappingRadius = comp.m_snappingRadius;
            this.UpgradeFromIndex = comp.UpgradeFromIndex;
            this.UpgradeIndex = comp.UpgradeIndex;

            if (comp.RequiredQuestEvents != null && comp.RequiredQuestEvents.Length > 0)
            {
                this.RequiredQuestEventUIDs = comp.RequiredQuestEvents.Select(it => it.EventUID).ToArray();
            }
        }
    }
}
