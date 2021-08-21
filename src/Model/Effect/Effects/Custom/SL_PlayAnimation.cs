using System;

namespace SideLoader
{
    public class SL_PlayAnimation : SL_Effect, ICustomModel
    {
        public Type GameModel => typeof(PlayAnimationEffect);
        public Type SLTemplateModel => typeof(SL_PlayAnimation);

        /// <summary>The animation you want to play</summary>
        public Character.SpellCastType Animation;
        /// <summary>The modifier on your animation</summary>
        public Character.SpellCastModifier Modifier;
        /// <summary>Sheathe state requied. 0 = not set, 1 = require unsheathe, 2 = require sheathe</summary>
        public int SheatheRequired;

        public override void ApplyToComponent<T>(T component)
        {
            var comp = component as PlayAnimationEffect;

            comp.SyncType = Effect.SyncTypes.Everyone;

            comp.Animation = this.Animation;
            comp.Modifier = this.Modifier;
            comp.SheatheRequired = this.SheatheRequired;
        }

        public override void SerializeEffect<T>(T effect)
        {
            var comp = effect as PlayAnimationEffect;

            this.Animation = comp.Animation;
            this.Modifier = comp.Modifier;
            this.SheatheRequired = comp.SheatheRequired;
        }
    }

    public class PlayAnimationEffect : Effect, ICustomModel
    {
        public Type GameModel => typeof(PlayAnimationEffect);
        public Type SLTemplateModel => typeof(SL_PlayAnimation);

        public Character.SpellCastType Animation;
        public Character.SpellCastModifier Modifier;
        public int SheatheRequired;

        public override void ActivateLocally(Character _affectedCharacter, object[] _infos)
        {
            _affectedCharacter.SpellCastAnim(Animation, Modifier, SheatheRequired);
        }
    }
}
