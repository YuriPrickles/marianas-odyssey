using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Mod.OdysseyHelper.Cutscenes;
using CelesteMod.Publicizer;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste;

public class LoveCutscene : CutsceneEntity
{
	public const string Flag = "badeline_connection";
	public Player player;
	public MerryanaSunsetCutsceneEntity badeline;
	public float fade;
	public float pictureFade;
	public float pictureGlow;
	public MTexture picture;
	public bool waitForKeyPress;
	public float timer;
	public EventInstance sfx;

	public LoveCutscene(Player player, MerryanaSunsetCutsceneEntity badeline)
	{
		base.Tag = Tags.HUD;
		this.player = player;
		this.badeline = badeline;
	}


	public override void OnBegin(Level level)
	{
		Add(new Coroutine(Cutscene(level)));
	}


	public IEnumerator Cutscene(Level level)
	{
		player.StateMachine.State = 11;
		player.StateMachine.Locked = true;
		while (!player.OnGround())
		{
			yield return null;
		}

		player.Facing = Facings.Right;
		yield return 1f;
		Level level2 = SceneAs<Level>();
		level2.Session.Audio.Music.Event = "event:/Prickles/OdysseyOfSand/Harmony";
		level2.Session.Audio.Apply(forceSixteenthNoteHack: false);
		yield return Textbox.Say("OOS_SUNSET_END", StartMusic, PlayerHug, BadelineCalmDown);
		yield return 0.5f;
		while ((fade += Engine.DeltaTime) < 1f)
		{
			yield return null;
		}

		picture = GFX.Portraits["Prickles/OdysseyOfSand/merryhug00"];
		sfx = Audio.Play("event:/game/06_reflection/hug_image_1");
		yield return PictureFade(1f);
		yield return WaitForPress();
		sfx = Audio.Play("event:/game/06_reflection/hug_image_2");
		yield return PictureFade(0f, 0.5f);
		picture = GFX.Portraits["Prickles/OdysseyOfSand/merryhug01"];
		yield return PictureFade(1f);
		yield return WaitForPress();
		sfx = Audio.Play("event:/game/06_reflection/hug_image_2");
		yield return PictureFade(0f, 0.5f);
		picture = GFX.Portraits["Prickles/OdysseyOfSand/merryhug02"];
		yield return PictureFade(1f);
		yield return WaitForPress();
		sfx = Audio.Play("event:/game/06_reflection/hug_image_3");
		while ((pictureGlow += Engine.DeltaTime / 2f) < 1f)
		{
			yield return null;
		}

		yield return 0.2f;
		yield return PictureFade(0f, 0.5f);
		while ((fade -= Engine.DeltaTime * 12f) > 0f)
		{
			yield return null;
		}
		yield return player.Sprite.PlayRoutine("wakeUp");
		level.Session.Audio.Music.Param("levelup", 1f);
		level.Session.Audio.Apply(forceSixteenthNoteHack: false);
		Add(new Coroutine(badeline.TurnWhite(1f)));
		yield return 0.5f;
		player.Sprite.Play("idle");
		yield return 0.25f;
		yield return player.DummyWalkToExact((int)player.X - 8, walkBackwards: true);
		//Add(new Coroutine(CenterCameraOnPlayer()));
		yield return badeline.Disperse();
		(Scene as Level).Session.SetFlag("badeline_connection");
		level.Flash(Color.White);
		level.Session.Inventory.Dashes = 2;
		badeline.RemoveSelf();
		yield return 0.1f;
		level.Add(new LevelUpEffect(player.Position));
		Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
		yield return 2f;
		yield return level.ZoomBack(0.5f);
		EndCutscene(level);
	}

	public IEnumerator StartMusic()
	{
		Level level = SceneAs<Level>();
		level.Session.Audio.Music.Event = "event:/Prickles/OdysseyOfSand/Harmony";
		level.Session.Audio.Apply(forceSixteenthNoteHack: false);
		yield return 0.5f;
		player.DummyAutoAnimate = false;
		player.Sprite.Play("carryTheoWalk");
	}


	public IEnumerator PlayerHug()
	{
		Add(new Coroutine(Level.ZoomTo(player.Center + new Vector2(0f, -24f) - Level.Camera.Position, 2f, 0.5f)));
		yield return 0.6f;
		yield return badeline.BetterMoveTo(new Vector2(player.X + 10,badeline.Y));
		badeline.Hair.Facing = Facings.Left;
		yield return 0.25f;
		badeline.PlayerSprite.Play("hug");
		yield return 0.5f;
	}


	public IEnumerator BadelineCalmDown()
	{
		badeline.LoopingSfx.Param("end", 1f);
		yield return 0.5f;
		Input.Rumble(RumbleStrength.Light, RumbleLength.Long);

		yield return 1.5f;
	}


	public IEnumerator CenterCameraOnPlayer()
	{
		yield return 0.5f;
		Vector2 from = Level.ZoomFocusPoint;
		Vector2 to = new Vector2(Level.Bounds.Left + 580, Level.Bounds.Top + 124) - Level.Camera.Position;
		for (float p = 0f; p < 1f; p += Engine.DeltaTime)
		{
			Level.ZoomFocusPoint = from + (to - from) * Ease.SineInOut(p);
			yield return null;
		}
	}



	public IEnumerator PictureFade(float to, float duration = 1f)
	{
		while ((pictureFade = Calc.Approach(pictureFade, to, Engine.DeltaTime / duration)) != to)
		{
			yield return null;
		}
	}


	public IEnumerator WaitForPress()
	{
		waitForKeyPress = true;
		while (!Input.MenuConfirm.Pressed)
		{
			yield return null;
		}

		waitForKeyPress = false;
	}


	public override void OnEnd(Level level)
	{
		if (WasSkipped && sfx != null)
		{
			Audio.Stop(sfx);
		}

		Audio.SetParameter(Audio.CurrentAmbienceEventInstance, "postboss", 0f);
		level.ResetZoom();
		level.Session.Inventory.Dashes = 2;
		level.Session.Audio.Music.Event = "event:/Prickles/OdysseyOfSand/Harmony";
		if (WasSkipped)
		{
			level.Session.Audio.Music.Param("levelup", 2f);
		}

		level.Session.Audio.Apply(forceSixteenthNoteHack: false);
		if (WasSkipped)
		{
			level.Add(new LevelUpEffect(player.Position));
		}

		player.DummyAutoAnimate = true;
		player.StateMachine.Locked = false;
		player.StateMachine.State = 0;
		FinalBossStarfield finalBossStarfield = Level.Background.Get<FinalBossStarfield>();
		if (finalBossStarfield != null)
		{
			finalBossStarfield.Alpha = 0f;
		}

		badeline.RemoveSelf();
		level.Session.SetFlag("badeline_connection");
	}


	public override void Update()
	{
		timer += Engine.DeltaTime;
		base.Update();
	}


	public override void Render()
	{
		if (!(fade > 0f))
		{
			return;
		}

		Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * Ease.CubeOut(fade) * 0.8f);
		if (picture != null && pictureFade > 0f)
		{
			float num = Ease.CubeOut(pictureFade);
			Vector2 position = new Vector2(960f, 540f);
			float scale = 1f + (1f - num) * 0.025f;
			picture.DrawCentered(position, Color.White * Ease.CubeOut(pictureFade), scale, 0f);
			if (pictureGlow > 0f)
			{
				GFX.Portraits["Prickles/OdysseyOfSand/merryhug04"].DrawCentered(position, Color.White * Ease.CubeOut(pictureFade * pictureGlow), scale);
				GFX.Portraits["Prickles/OdysseyOfSand/merryhug05"].DrawCentered(position, Color.White * Ease.CubeOut(pictureFade * pictureGlow), scale);
				GFX.Portraits["Prickles/OdysseyOfSand/merryhug06"].DrawCentered(position, Color.White * Ease.CubeOut(pictureFade * pictureGlow), scale);
				HiresRenderer.EndRender();
				HiresRenderer.BeginRender(BlendState.Additive);
				GFX.Portraits["Prickles/OdysseyOfSand/merryhug04"].DrawCentered(position, Color.White * Ease.CubeOut(pictureFade * pictureGlow), scale);
				GFX.Portraits["Prickles/OdysseyOfSand/merryhug05"].DrawCentered(position, Color.White * Ease.CubeOut(pictureFade * pictureGlow), scale);
				GFX.Portraits["Prickles/OdysseyOfSand/merryhug06"].DrawCentered(position, Color.White * Ease.CubeOut(pictureFade * pictureGlow), scale);
				HiresRenderer.EndRender();
				HiresRenderer.BeginRender();
			}

			if (waitForKeyPress)
			{
				GFX.Gui["textboxbutton"].DrawCentered(new Vector2(1520f, 880 + ((timer % 1f < 0.25f) ? 6 : 0)));
			}
		}
	}
}