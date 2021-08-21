using UnityEngine;

namespace SideLoader
{
    public class SL_Summon : SL_Effect
    {
        public const string CorruptionSpiritPath = "CorruptionSpirit.prefab";
        public const string SummonGhostPath = "NewGhostOneHandedAlly.prefab";

        public enum PrefabTypes
        {
            Item,
            Resource
        }

        // if Item: the ItemID, if Character: the 'Resources/' asset path.
        public string Prefab;
        public PrefabTypes SummonPrefabType;
        public int BufferSize = 1;
        public bool LimitOfOne;
        public Summon.InstantiationManagement SummonMode;
        public Summon.SummonPositionTypes PositionType;
        public float MinDistance;
        public float MaxDistance;
        public bool SameDirectionAsSummoner;
        public Vector3 SummonLocalForward;
        public bool IgnoreOnDestroy;

        public override void ApplyToComponent<T>(T component)
        {
            var comp = component as Summon;

            if (this.SummonPrefabType == PrefabTypes.Resource)
            {
                if (Resources.Load<GameObject>(Prefab) is GameObject prefab)
                {
                    comp.SummonedPrefab = prefab.transform;
                }
                else
                {
                    SL.Log("Could not find Resources asset: " + this.Prefab + "!");
                    return;
                }
            }
            else
            {
                if (int.TryParse(this.Prefab, out int id) && ResourcesPrefabManager.Instance.GetItemPrefab(id) is Item item)
                {
                    comp.SummonedPrefab = item.transform;
                }
                else
                {
                    SL.Log("Could not find an Item with the ID " + this.Prefab + ", or that is not a valid ID!");
                    return;
                }
            }

            comp.BufferSize = this.BufferSize;
            comp.LimitOfOne = this.LimitOfOne;
            comp.InstantiationMode = this.SummonMode;
            comp.PositionType = this.PositionType;
            comp.MinDistance = this.MinDistance;
            comp.MaxDistance = this.MaxDistance;
            comp.SameDirAsSummoner = this.SameDirectionAsSummoner;
            comp.SummonLocalForward = this.SummonLocalForward;
            comp.IgnoreOnDestroy = this.IgnoreOnDestroy;
        }

        public override void SerializeEffect<T>(T effect)
        {
            var comp = effect as Summon;

            if (effect is SummonAI)
            {
                SummonPrefabType = PrefabTypes.Resource;

                var name = comp.SummonedPrefab.name;
                if (name.EndsWith("(Clone)"))
                {
                    name = name.Substring(0, name.Length - 7);
                }

                Prefab = name;
            }
            else
            {
                SummonPrefabType = PrefabTypes.Item;

                Prefab = comp.SummonedPrefab.gameObject.GetComponent<Item>().ItemID.ToString();
            }

            BufferSize = comp.BufferSize;
            LimitOfOne = comp.LimitOfOne;
            SummonMode = comp.InstantiationMode;
            PositionType = comp.PositionType;
            MinDistance = comp.MinDistance;
            MaxDistance = comp.MaxDistance;
            SameDirectionAsSummoner = comp.SameDirAsSummoner;
            SummonLocalForward = comp.SummonLocalForward;
            IgnoreOnDestroy = comp.IgnoreOnDestroy;
        }
    }
}
