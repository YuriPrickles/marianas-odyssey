using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using System.Collections;
using static Celeste.WindController;

namespace Celeste.Mod.OdysseyHelper.Cutscenes
{
    [CustomEvent("OdysseyOfSand/Nightmare")]
    public class NightmareCutscene : CutsceneEntity
    {
        bool SecondPart = false;
        Player player;
        public NightmareCutscene(bool secondPart, Player plr)
        {
            SecondPart = secondPart;
            player = plr;
        }
        public NightmareCutscene(EventTrigger trigger, Player player, string eventID) : base()
        {
            this.player = player;
        }

        public override void OnBegin(Level level)
        {
            level.PauseLock = true;
            if (!SecondPart)
                Add(new Coroutine(RestStart(level)));
            else
                Add(new Coroutine(MainCutscene(level)));
        }
        public override void OnEnd(Level level)
        {
            if (!level.Session.GetFlag("tea_obtained") || level.Session.GetFlag("nightmare_done"))
            {
                player.StateMachine.State = Player.StNormal;
                level.PauseLock = false;
                return;
            }
            if (!SecondPart)
            {
                new FadeWipe(level, false, () =>
                {
                    Leader.StoreStrawberries(player.Leader);
                    level.Remove(player);
                    level.Session.Level = "0-nightmare";
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                    level.LoadLevel(Player.IntroTypes.None);
                    level.Add(new NightmareCutscene(true, player));
                })
                { Duration = 1.4f };
            }
            else
            {
                level.Session.SetFlag("nightmare_done");
                player.StateMachine.State = Player.StNormal;
                level.PauseLock = false;
                player.DummyAutoAnimate = true;
            }

        }

        public IEnumerator RestStart(Level level)
        {
            if (!level.Session.GetFlag("tea_obtained") || level.Session.GetFlag("nightmare_done"))
            {
                EndCutscene(level);
                yield break;
            }
            WindController windController = Scene.Entities.FindFirst<WindController>();
            if (windController == null)
            {
                windController = new WindController(Patterns.None);
                Scene.Add(windController);
            }
            else
            {
                windController.SetPattern(Patterns.None);
            }
            player.StateMachine.State = Player.StDummy;

            yield return 1f;
            yield return Textbox.Say("OOS_TOWERS_REST");
            EndCutscene(level);
        }

        public IEnumerator MainCutscene(Level level)
        {
            if (!level.Session.GetFlag("tea_obtained") || level.Session.GetFlag("nightmare_done"))
            {
                EndCutscene(level);
                yield break;
            }
            player = level.Tracker.GetEntity<Player>();
            Leader.RestoreStrawberries(level.Tracker.GetEntity<Player>().Leader);
            player.StateMachine.State = Player.StDummy;
            player.DummyAutoAnimate = false;
            player.Sprite.Play("asleep");

            Add(new Coroutine(level.ZoomTo(new Vector2(190.4f, 176f), 3.684f, 0f)));
            yield return 2f;
            yield return Textbox.Say("OOS_TOWERS_NIGHTMARE", Crash);
            yield return 2f;
            player.Sprite.Play("wakeUp");
            yield return level.ZoomBack(5f);
            EndCutscene(level);
        }

        FakeCrashScreen crashScreen;
        public IEnumerator Crash()
        {
            crashScreen = new FakeCrashScreen(player);
            Scene.Add(crashScreen);
            while (!crashScreen.reallyDone)
                yield return null;
        }
    }
}
