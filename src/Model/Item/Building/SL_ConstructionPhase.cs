using System.Collections.Generic;
using System.Linq;

namespace SideLoader
{
    [SL_Serialized]
    public class SL_ConstructionPhase
    {
        public string Name;
        public Building.ConstructionPhase.Type PhaseType;

        public BuildingResourceValues ConstructionCosts;
        public int ConstructionTime;

        public int RareMaterialID;

        public List<SL_BuildingRequirement> BuildingRequirements;

        public int HouseCountRequirement;
        public int HousingValue;
        public bool MultiplyProductionPerHouse;

        public BuildingResourceValues CapacityBonus;
        public BuildingResourceValues UpkeepCosts;
        public BuildingResourceValues UpkeepProductions;

        public Building.ConstructionPhase GeneratePhase()
        {
            var ret = new Building.ConstructionPhase
            {
                CapacityBonus = this.CapacityBonus,
                ConstructionType = this.PhaseType,
                HouseCountRequirements = this.HouseCountRequirement,
                HousingValue = this.HousingValue,
                MultiplyProductionPerHouse = this.MultiplyProductionPerHouse,
                RareMaterialRequirement = new ItemQuantity(ResourcesPrefabManager.Instance.GetItemPrefab(this.RareMaterialID), 1),
                UpkeepCosts = this.UpkeepCosts,
                UpkeepProductions = this.UpkeepProductions,
            };

            var nameLoc = $"BuildPhaseLoc_{UID.Generate()}";
            References.GENERAL_LOCALIZATION.Add(nameLoc, this.Name);
            ret.NameLocKey = nameLoc;

            var reqList = new List<Building.BuildingRequirement>();
            foreach (var req in this.BuildingRequirements)
            {
                reqList.Add(new Building.BuildingRequirement
                {
                    ReqBuilding = ResourcesPrefabManager.Instance.GetItemPrefab(req.ReqBuildingID) as Building,
                    UpgradeIndex = req.ReqUpgradeIndex,
                });
            }
            ret.BuildingRequirements = reqList.ToArray();

            return ret;
        }

        public static SL_ConstructionPhase SerializePhase(Building.ConstructionPhase phase)
        {
            return new SL_ConstructionPhase
            {
                Name = LocalizationManager.Instance.GetLoc(phase.NameLocKey),
                PhaseType = phase.ConstructionType,
                HouseCountRequirement = phase.HouseCountRequirements,
                ConstructionTime = phase.GetConstructionTime(),
                ConstructionCosts = phase.m_constructionCosts,
                UpkeepCosts = phase.UpkeepCosts,
                UpkeepProductions = phase.UpkeepProductions,
                CapacityBonus = phase.CapacityBonus,
                MultiplyProductionPerHouse = phase.MultiplyProductionPerHouse,
                HousingValue = phase.HousingValue,
                RareMaterialID = phase.RareMaterial?.ItemID ?? -1,
                BuildingRequirements = (from req in phase.BuildingRequirements
                                        select new SL_BuildingRequirement()
                                        {
                                            ReqBuildingID = req.ReqBuilding?.ItemID ?? -1,
                                            ReqUpgradeIndex = req.UpgradeIndex
                                        }).ToList(),
            };
        }
    }
}
