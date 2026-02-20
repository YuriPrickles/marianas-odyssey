using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.OdysseyHelper.Cutscenes
{
	[CustomEvent("OdysseyEntities/AbbyMirage")]
	public class AbbyMirage : CutsceneEntity
	{
		PatientBadelineOldsite abby;
        private Player player;
        Vector2 abbySpawn = CutsceneNode.Find("AbbySpawn").Center;
        Vector2 abby_to = CutsceneNode.Find("AbbySpawn").Center - new Vector2(0,16);
        public AbbyMirage(EventTrigger trigger, Player player, string eventID) : base()
		{
			this.player = player;
		}
        public override void OnBegin(Level level)
        {
            abby = new PatientBadelineOldsite(abbySpawn, 0);
            abby.Visible = true;
            abby.Hovering = false;
            level.Add(abby);
            Add(new Coroutine(cutscene(level)));
		}
		private IEnumerator cutscene(Level level)
        {
            if (!level.Session.GetFlag("abby_intro"))
            {
                Audio.Play("event:/game/02_old_site/sequence_badeline_intro", abbySpawn);
                Level.Displacement.AddBurst(abby.Center, 0.8f, 8f, 48f, 0.5f);
                abby.Visible = true;
                player.StateMachine.State = Player.StDummy;
                yield return Textbox.Say("OOS_MIRAGE_FAKEWIFE", ZoomIn, GetUp, Transform);
                yield return Level.ZoomBack(0.5f);
                EndCutscene(Level); //Tells the level the cutscene has been completed and calls "OnEnd".
            }
            else
            {
                abby.Hovering = true;
                Audio.Play("event:/game/02_old_site/sequence_badeline_intro", abbySpawn);
                Level.Displacement.AddBurst(abby.Center, 0.8f, 8f, 48f, 0.5f);
                EndCutscene(Level); //Tells the level the cutscene has been completed and calls "OnEnd".
            }
        }
		IEnumerator ZoomIn()
        {
            yield return 0.2f;
            yield return Level.ZoomTo(new Vector2(160, 100), 1.4f, 0.5f);
            yield return 0.2f;
        }
        IEnumerator GetUp()
        {
            abby.Visible = true;
            abby.trueSprite.Play("lookback");
            yield return 1f;
        }
        IEnumerator Transform()
        {
            abby.Visible = true;
            abby.trueSprite.Play("transform");
            for (float t = 0f; t < 1f; t += Engine.DeltaTime)
            {
                abby.Position = abbySpawn + (abby_to - abbySpawn) * Ease.CubeInOut(t);
                yield return null;
            }
            abby.Hovering = true;
            yield return 2f;
            abby.trueSprite.FlipX = true;
        }
        public override void OnEnd(Level level)
        {
            SceneAs<Level>().Session.SetFlag("abby_intro");
            abby.trueSprite.FlipX = false;
            abby.Visible = true;
            abby.trueSprite.Play("idle");
            abby.Hovering = true;

            if (WasSkipped)
			{
                abby.Position = abby_to;

            }
            abby.Add(new Coroutine(abby.StartChasingRoutine(level)));
            player.StateMachine.State = Player.StNormal;
		}
	}
}