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
    [CustomEvent("OdysseyOfSand/SandstormHeartTease")]
    public class SandstormHeartTease : CutsceneEntity
    {
        BadelineDummy badeline;
        Player player;
        FakeHeart fakeHeart;
        Vector2 heartspawn = CutsceneNode.Find("heartspawn").Position;
        Vector2 runoffTo = CutsceneNode.Find("runoffto").Position;
        public SandstormHeartTease(EventTrigger trigger, Player player, string eventID) : base()
        {
            this.player = player;
            fakeHeart = new FakeHeart(heartspawn);
        }
        public override void OnBegin(Level level)
        {
            if (level.Session.GetFlag("seen_heart"))
            {
                return;
            }
            level.Add(fakeHeart);
            Add(new Coroutine(Cutscene(level)));
        }
        
        public IEnumerator Cutscene(Level level)
        {
            while (!player.onGround)
                yield return null;
            player.StateMachine.State = Player.StDummy;
            Level.Session.Inventory.Dashes = 1;
            yield return 0.3f;
            player.Dashes = 1;
            Vector2 vector = player.Position + new Vector2(-12f, -10f);
            Level.Displacement.AddBurst(vector, 0.5f, 8f, 32f, 0.5f);
            Level.Add(badeline = new BadelineDummy(vector));
            Audio.Play("event:/char/badeline/maddy_split", vector);
            badeline.Sprite.Scale.X = 1f;
            yield return badeline.FloatTo(vector + new Vector2(0f, -6f), 1, faceDirection: false);
            yield return Textbox.Say("OOS_DESCENT_CHP6", FocusHeart);
            yield return BadelineRejoin();
            EndCutscene(level);
        }
        public IEnumerator BadelineRejoin()
        {
            Audio.Play("event:/new_content/char/badeline/maddy_join_quick", badeline.Position);
            Vector2 from = badeline.Position;
            for (float p = 0f; p < 1f; p += Engine.DeltaTime / 0.25f)
            {
                badeline.Position = Vector2.Lerp(from, player.Position, Ease.CubeIn(p));
                yield return null;
            }

            Level.Displacement.AddBurst(player.Center, 0.5f, 8f, 32f, 0.5f);
            Level.Session.Inventory.Dashes = 2;
            player.Dashes = 2;
            badeline.RemoveSelf();
        }

        public IEnumerator FocusHeart()
        {
            yield return CameraTo(fakeHeart.Position - new Vector2(160, 90), 3);
            Audio.Play("event:/game/06_reflection/badeline_freakout_" + Random.Shared.Next(1, 3).ToString());
            SceneAs<Level>().Displacement.AddBurst(fakeHeart.Position, 1f, 16f, 300f, 1f);
            yield return 3f;
            Audio.Play("event:/game/general/strawberry_laugh", fakeHeart.Position).setPitch(1.2f);
            Add(new Coroutine(SpeedRings()));
            MoveHeart(runoffTo, Ease.QuintIn, 5);
            yield return 2f;
            yield return CameraTo(player.Position - new Vector2(160, 90), 0.7f);
        }
        public IEnumerator SpeedRings()
        {
            yield return 0.5f;
            while (fakeHeart != null)
            {
                Level.Add(Engine.Pooler.Create<SpeedRing>().Init(fakeHeart.Center, new Vector2(0f, 1f).Angle(), Color.White));
                yield return 0.05f;
            }
        }
        public void MoveHeart(Vector2 target, Ease.Easer easing, float duration)
        {
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, easing, duration, start: true);
            tween.OnUpdate = (Tween t) =>
            {
                fakeHeart.Position = (Vector2.Lerp(fakeHeart.Position, target, t.Eased));
            };
            Add(tween);
        }
        public override void OnEnd(Level level)
        {
            level.Session.SetFlag("seen_heart");
            player.StateMachine.State = Player.StNormal;
            Level.Session.Inventory.Dashes = 2;
            player.Dashes = 2;
            fakeHeart?.Remove();
            badeline?.Remove();
        }
    }
}
