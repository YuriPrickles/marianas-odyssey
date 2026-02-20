using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using Celeste.Mod.Meta;
using System.Collections.ObjectModel;

namespace Celeste.Mod.OdysseyHelper
{
    [CustomEntity("OdysseyOfSand/NightmareHeart")]
    public class NightmareHeart(EntityData data, Vector2 offset) : HeartGem(data, offset)
    {
        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            Remove(Get<PlayerCollider>());
            Add(new PlayerCollider(NewOnPlayer));
        }
        public void NewOnPlayer(Player player)
        {
            if (collected || (base.Scene as Level).Frozen)
            {
                return;
            }

            if (player.DashAttacking)
            {
                NewCollect(player);
                return;
            }

            if (bounceSfxDelay <= 0f)
            {
                if (IsFake)
                {
                    Audio.Play("event:/new_content/game/10_farewell/fakeheart_bounce", Position);
                }
                else
                {
                    Audio.Play("event:/game/general/crystalheart_bounce", Position);
                }

                bounceSfxDelay = 0.1f;
            }

            player.PointBounce(base.Center);
            moveWiggler.Start();
            ScaleWiggler.Start();
            moveWiggleDir = (base.Center - player.Center).SafeNormalize(Vector2.UnitY);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        }
        public void NewCollect(Player player)
        {
            base.Scene.Tracker.GetEntity<AngryOshiro>()?.StopControllingTime();
            Coroutine coroutine = new Coroutine(NewCollectRoutine(player));
            coroutine.UseRawDeltaTime = true;
            Add(coroutine);
            collected = true;
            if (!removeCameraTriggers)
            {
                return;
            }

            foreach (CameraOffsetTrigger item in base.Scene.Entities.FindAll<CameraOffsetTrigger>())
            {
                item.RemoveSelf();
            }
        }
        private bool NewIsCompleteArea(bool value)
        {
            MapMetaModeProperties meta = ((base.Scene as Level)?.Session.MapData).Meta;
            if (meta != null && meta.HeartIsEnd.HasValue)
            {
                if (meta.HeartIsEnd.Value)
                {
                    return !IsFake;
                }

                return false;
            }

            return value;
        }
        public new IEnumerator NewCollectRoutine(Player player)
        {
            Level level = Scene as Level;
            bool flag = false;
            MapMetaModeProperties mapMetaModeProperties = ((level != null) ? level.Session.MapData.Meta : null);
            if (mapMetaModeProperties != null && mapMetaModeProperties.HeartIsEnd.HasValue)
            {
                flag = mapMetaModeProperties.HeartIsEnd.Value;
            }

            if (flag & !IsFake)
            {
                List<IStrawberry> list = new List<IStrawberry>();
                ReadOnlyCollection<Type> berryTypes = StrawberryRegistry.GetBerryTypes();
                foreach (Follower follower in player.Leader.Followers)
                {
                    if (berryTypes.Contains(follower.Entity.GetType()) && follower.Entity is IStrawberry)
                    {
                        list.Add(follower.Entity as IStrawberry);
                    }
                }

                foreach (IStrawberry item in list)
                {
                    item.OnCollect();
                }
            }
            AreaKey area = level.Session.Area;
            string poemID = AreaData.Get(level).Mode[(int)area.Mode].PoemID;
            bool completeArea = NewIsCompleteArea(!IsFake && (area.Mode != 0 || area.ID == 9));
            if (IsFake)
            {
                level.StartCutscene(SkipFakeHeartCutscene);
            }
            else
            {
                level.CanRetry = false;
            }

            if (completeArea || IsFake)
            {
                Audio.SetMusic(null);
                Audio.SetAmbience(null);
            }

            if (completeArea)
            {
                List<Strawberry> list = new List<Strawberry>();
                foreach (Follower follower in player.Leader.Followers)
                {
                    if (follower.Entity is Strawberry)
                    {
                        list.Add(follower.Entity as Strawberry);
                    }
                }

                foreach (Strawberry item in list)
                {
                    item.OnCollect();
                }
            }

            string text = "event:/game/general/crystalheart_blue_get";
            if (IsFake)
            {
                text = "event:/new_content/game/10_farewell/fakeheart_get";
            }
            else if (area.Mode == AreaMode.BSide)
            {
                text = "event:/game/general/crystalheart_red_get";
            }
            else if (area.Mode == AreaMode.CSide)
            {
                text = "event:/game/general/crystalheart_gold_get";
            }

            sfx = SoundEmitter.Play(text, this);
            Add(new LevelEndingHook(delegate
            {
                sfx.Source.Stop();
            }));
            walls.Add(new InvisibleBarrier(new Vector2(level.Bounds.Right, level.Bounds.Top), 8f, level.Bounds.Height));
            walls.Add(new InvisibleBarrier(new Vector2(level.Bounds.Left - 8, level.Bounds.Top), 8f, level.Bounds.Height));
            walls.Add(new InvisibleBarrier(new Vector2(level.Bounds.Left, level.Bounds.Top - 8), level.Bounds.Width, 8f));
            foreach (InvisibleBarrier wall in walls)
            {
                Scene.Add(wall);
            }

            Add(white = GFX.SpriteBank.Create("heartGemWhite"));
            Depth = -2000000;
            yield return null;
            Celeste.Freeze(0.2f);
            yield return null;
            Engine.TimeRate = 0.5f;
            player.Depth = -2000000;
            for (int i = 0; i < 10; i++)
            {
                Scene.Add(new AbsorbOrb(Position));
            }

            level.Shake();
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            level.Flash(Color.White);
            level.FormationBackdrop.Display = true;
            level.FormationBackdrop.Alpha = 1f;
            light.Alpha = (bloom.Alpha = 0f);
            Visible = false;
            for (float t3 = 0f; t3 < 2f; t3 += Engine.RawDeltaTime)
            {
                Engine.TimeRate = Calc.Approach(Engine.TimeRate, 0f, Engine.RawDeltaTime * 0.25f);
                yield return null;
            }

            yield return null;
            if (player.Dead)
            {
                yield return 100f;
            }

            Engine.TimeRate = 1f;
            Tag = Tags.FrozenUpdate;
            level.Frozen = true;
            if (!IsFake)
            {
                RegisterAsCollected(level, poemID);
                if (completeArea)
                {
                    level.TimerStopped = true;
                    level.RegisterAreaComplete();
                }
            }

            string text2 = null;
            if (!string.IsNullOrEmpty(poemID))
            {
                text2 = Dialog.Clean("poem_" + poemID);
            }

            poem = new Poem(text2, (int)(IsFake ? ((AreaMode)3) : area.Mode), (area.Mode == AreaMode.CSide || IsFake) ? 1f : 0.6f);
            poem.Alpha = 0f;
            Scene.Add(poem);
            for (float t3 = 0f; t3 < 1f; t3 += Engine.RawDeltaTime)
            {
                poem.Alpha = Ease.CubeOut(t3);
                yield return null;
            }

            if (IsFake)
            {
                yield return DoFakeRoutineWithBird(player);
                yield break;
            }

            while (!Input.MenuConfirm.Pressed && !Input.MenuCancel.Pressed)
            {
                yield return null;
            }

            sfx.Source.Param("end", 1f);
            if (!completeArea)
            {
                level.FormationBackdrop.Display = false;
                for (float t3 = 0f; t3 < 1f; t3 += Engine.RawDeltaTime * 2f)
                {
                    poem.Alpha = Ease.CubeIn(1f - t3);
                    yield return null;
                }

                player.Depth = 0;
                Leader.StoreStrawberries(player.Leader);
                level.Remove(player);
                level.Session.Level = "2d-withcloudstoo";
                level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(8648, -120));
                level.LoadLevel(Player.IntroTypes.WakeUp);
                Leader.RestoreStrawberries(level.Tracker.GetEntity<Player>().Leader);
                EndCutscene();
            }
            else
            {
                FadeWipe fadeWipe = new FadeWipe(level, wipeIn: false);
                fadeWipe.Duration = 3.25f;
                yield return fadeWipe.Duration;
                level.CompleteArea(spotlightWipe: false, skipScreenWipe: true, skipCompleteScreen: false);
            }
        }
    }
}
