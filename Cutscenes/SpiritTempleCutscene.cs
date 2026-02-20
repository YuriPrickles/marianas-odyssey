using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using CelesteMod.Publicizer;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.OdysseyHelper.Cutscenes;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.Entities;
using System;

namespace Celeste.Mod.OdysseyHelper.Cutscenes
{
    [CustomEvent("OdysseyOfSand/SpiritTempleCutscene")]
    public class SpiritTempleCutscene : CutsceneEntity
    {
        BadelineDummy badeline;
        Player player;
        public SpiritTempleCutscene(EventTrigger trigger, Player player, string eventID) : base()
        {
            this.player = player;
        }
        public override void OnBegin(Level level)
        {
            if (level.Session.GetFlag("met_merry"))
            {
                return;
            }
            Add(new Coroutine(Cutscene(level)));
        }
        
        public IEnumerator Cutscene(Level level)
        {
            Vector2 vector = player.Position + new Vector2(40f, 0f);
            while (!player.onGround)
                yield return null;
            player.StateMachine.State = Player.StDummy;
            Level.Displacement.AddBurst(vector, 0.5f, 8f, 32f, 0.5f);
            Level.Add(badeline = new BadelineDummy(vector));
            badeline.Appear(Level);
            badeline.Sprite.Scale.X = -1f;
            yield return Textbox.Say("OOS_TEMPLE_MERRYANA");
            badeline.Vanish();
            EndCutscene(level);
        }

        public override void OnEnd(Level level)
        {
            level.Session.SetFlag("met_merry");
            player.StateMachine.State = Player.StNormal;
            badeline?.Remove();
        }
    }
}
