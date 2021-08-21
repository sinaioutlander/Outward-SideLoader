using SideLoader.SLPacks.Categories;
using System;
using System.IO;

namespace SideLoader
{
    public class SL_LevelAttackSkill : SL_AttackSkill
    {
        public string WatchedStatusIdentifier;
        public SL_SkillStage[] Stages;

        public override void ApplyToItem(Item item)
        {
            base.ApplyToItem(item);

            var comp = item as LevelAttackSkill;

            if (this.WatchedStatusIdentifier != null)
            {
                var status = ResourcesPrefabManager.Instance.GetStatusEffectPrefab(this.WatchedStatusIdentifier);
                if (status)
                    comp.WatchedStatus = status;
                else
                    SL.LogWarning("SL_LevelAttackSkill: could not find a status by the name of '" + this.WatchedStatusIdentifier + "'");
            }

            var stages = comp.m_skillStages;
            int newMax = this.Stages?.Length ?? stages.Length;

            if (this.Stages != null)
            {
                if (stages.Length != newMax)
                    Array.Resize(ref stages, newMax);

                for (int i = 0; i < stages.Length; i++)
                {
                    var stage = stages[i];
                    stage.StageDefaultName = Stages[i].Name;
                    stage.StageLocKey = null;
                    stage.StageAnim = Stages[i].Animation;
                    if (!stage.StageIcon)
                        stage.StageIcon = comp.ItemIcon;
                }

            }

            // check for custom level icons
            if (!string.IsNullOrEmpty(SLPackName) && !string.IsNullOrEmpty(SubfolderName) && SL.GetSLPack(SLPackName) is SLPack pack)
            {
                var dir = Path.Combine(pack.GetPathForCategory<ItemCategory>(), SubfolderName, "Textures");

                for (int i = 0; i < newMax; i++)
                {
                    if (pack.FileExists(dir, $"icon{i + 2}.png"))
                    {
                        var tex = pack.LoadTexture2D(dir, $"icon{i + 2}.png");
                        var sprite = CustomTextures.CreateSprite(tex, CustomTextures.SpriteBorderTypes.NONE);

                        stages[i].StageIcon = sprite;
                    }
                }
            }

            comp.m_skillStages = stages;
        }

        public override void SerializeItem(Item item)
        {
            base.SerializeItem(item);

            var comp = item as LevelAttackSkill;

            WatchedStatusIdentifier = comp.WatchedStatus?.IdentifierName;

            var stages = comp.m_skillStages;
            if (stages != null)
            {
                this.Stages = new SL_SkillStage[stages.Length];
                for (int i = 0; i < stages.Length; i++)
                {
                    var stage = stages[i];
                    this.Stages[i] = new SL_SkillStage
                    {
                        Name = stage.GetName(),
                        Animation = stage.StageAnim
                    };
                }
            }
        }
    }

    [SL_Serialized]
    public class SL_SkillStage
    {
        public string Name;
        public Character.SpellCastType Animation;
    }
}
