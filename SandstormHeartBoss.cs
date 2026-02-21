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
using FMOD.Studio;
using static Celeste.MoonGlitchBackgroundTrigger;
using static Celeste.BirdTutorialGui;

namespace Celeste.Mod.OdysseyHelper;

[CustomEntity("OdysseyOfSand/SandstormHeartBoss")]
public class SandstormHeartBoss : HeartGem
{
	public enum Attacks
	{
		Idle,
		Slam,
		Whirlwind
	}
	public enum Phase
	{
		Intact,
		Chipped,
		Cracked,
		Shattering,
		LastHit
	}
	public Phase currentPhase = Phase.Intact;
	public CutsceneNode left;
	public CutsceneNode center;
	public CutsceneNode right;
	public CutsceneNode leftB;
	public CutsceneNode rightB;
	public CutsceneNode ground;
	public CutsceneNode heaven;
	public List<CutsceneNode> nodes;
	public BossHealthRenderer hpRenderer;
	public int health = 10000;
	public int maxhealth = 10000;
	public Coroutine patternCoroutine;
	public Coroutine flashCor;
	public Vector2 velocity = Vector2.Zero;
	public Vector2 targetPosition = Vector2.Zero;
	public float speed;
	public SineWave sine;
	public Vector2 anchor = Vector2.Zero;
	public bool wiggle = true;
	public HPController playerHP;
	public readonly int ChipPhaseThreshold = 9900;
	public readonly int CrackPhaseThreshold = 8800;
	public readonly int ShatterPhaseThreshold = 6500;
	public bool beatdownCutscene = false;
	public int beatdownHitCount = 10;
	public TimeRateModifier timeWarper;
	public int beatdownCutsceneDir = 1;
	public bool arriving = true;
	public Random random;
	public bool fightDone = false;
	public SandstormHeartBoss(EntityData data, Vector2 offset) : base(data,offset)
	{
		orig_ctor(data, offset);
		autoPulse = true;
		walls = new List<InvisibleBarrier>();
		Add(holdableCollider = new HoldableCollider(OnHoldable));
		Add(new MirrorReflection());
		Add(sine = new SineWave(0.44f, 0f).Randomize());
		anchor = Position;
		Add(patternCoroutine = new Coroutine(removeOnComplete: false));
		Add(flashCor = new Coroutine(removeOnComplete: false));
		UpdatePosition();
		random = new Random(Calc.Random.Next());
	}
	public bool IsPastPhase(Phase phase)
	{
		return ((int)currentPhase >= (int)phase);
	}
	public class BossHealthRenderer : Entity
	{
		public SandstormHeartBoss boss;

		public BossHealthRenderer(SandstormHeartBoss b)
		{
			boss = b;
			Tag = Tags.HUD;
		}
		public override void Update()
		{
			base.Update();
		}
		public override void Render()
        {
            string text2 = Dialog.Clean("HEARTBOSS_NAME");
            base.Render();
            Vector2 renderPos2 = new Vector2(Celeste.ViewWidth / 2, Celeste.ViewHeight * 0.3f);
            if (!Scene.Paused)
            {
                ActiveFont.DrawOutline(text2, renderPos2, Vector2.One / 2, Vector2.One * 3.5f, Color.White * boss.introTextAlpha, 2, Color.Black * boss.introTextAlpha);

            }
            Vector2 bossPos = boss.SceneAs<Level>().WorldToScreen(boss.Position);
			Vector2 renderPos = Calc.Abs(bossPos) + new Vector2(0,-64);
			string text = $"{Math.Round(((float)boss.health / (float)boss.maxhealth) * 100f,2)}%";
			if (!Scene.Paused && boss.Visible && !boss.arriving)
				ActiveFont.DrawOutline(text, renderPos,Vector2.One/2, Vector2.One, Color.White, 2, Color.Black);
			base.Render();
		}
	}
	public override void Awake(Scene scene)
	{
		base.Awake(scene);
		left = CutsceneNode.Find("Left");
		center = CutsceneNode.Find("Center");
		right = CutsceneNode.Find("Right");
		leftB = CutsceneNode.Find("LeftBounds");
		rightB = CutsceneNode.Find("RightBounds");
		heaven = CutsceneNode.Find("Heaven");
		nodes = [left, center, right];
		ground = CutsceneNode.Find("Ground");
		scene.Add(hpRenderer = new BossHealthRenderer(this));
		scene.Add(playerHP = new HPController(100));
		Remove(Get<PlayerCollider>());
		Add(new PlayerCollider(OnPlayer));
		Remove(bloom);
		Add(bloom = new BloomPoint(0.3f, 16f));
		Remove(sprite);
		Add(sprite = GFX.SpriteBank.Create("heartgem0"));
		Add(timeWarper = new TimeRateModifier(1));
		sprite.Play("spin");
		sprite.Rate = 2;
		sprite.OnLoop = (string anim) =>
		{
			if (Visible && anim == "spin" && autoPulse)
			{
				Audio.Play("event:/game/general/crystalheart_pulse", Position);

				ScaleWiggler.Start();
				(base.Scene as Level).Displacement.AddBurst(Position, 0.35f, 8f, 48f, 0.25f);
			}
		};
    }
	public bool initialUpdate = false;
    public float sineXMult = 9f;
	public float sineYMult = 8f;
	public void UpdatePosition()
	{
		if (wiggle)
			Position = new Vector2((float)(anchor.X + sine.Value * sineXMult), (float)(anchor.Y + sine.ValueOverTwo * sineYMult));
		else
			Position = anchor;
	}
	public float slamWait = 0.2f;
	public float slamDuration = 0.4f;
	public int whirlwindBalls = 3;
	public int whirlwindAmount = 12;
	public Vector2 whirlSine = new Vector2(24f, 2f);
	public float whirlSineFreq = 2.4f;
	public Vector2 idleSine = new Vector2(9f, 8f);
	public float idleSineFreq = 0.44f;
	public int extraBalls = 0;
	public override void Update()
	{
		if (!initialUpdate)
        {
            anchor = heaven.Position;
            arriving = !SceneAs<Level>().Session.GetFlag("seen_arrival");
            if (!arriving)
            {
                (base.Scene as Level).Displacement.AddBurst(Position, 0.35f, 8f, 48f, 0.25f);
                anchor = center.Position;
                Audio.SetMusic("event:/Prickles/OdysseyOfSand/heartboss");
                Audio.SetMusicParam("b_side", 0);
                introTextAlpha = 0;
            }
			initialUpdate = true;
        }
		base.Update();
		Player player = Scene.Tracker.GetEntity<Player>();
		if (player == null || player.Dead || !player.Active) return;
		if (patternCoroutine != null && !patternCoroutine.Active)
        {
            if (fightDone)
            {
                patternCoroutine.Cancel();
                return;
            }
            patternCoroutine.Replace(Pattern(player));
		}
		switch (currentPhase)
		{
			case Phase.Intact:
				slamWait = 0.2f;
				slamDuration = 0.4f;
				whirlwindBalls = 3;
				whirlwindAmount = 12;
				whirlSine = new Vector2(24f, 2f);
				whirlSineFreq = 2.4f;
				idleSine = new Vector2(9f, 8f);
				idleSineFreq = 0.44f;
				extraBalls = 0;
				break;
			case Phase.Chipped:
				slamWait = 0.16f;
				slamDuration = 0.3f;
				whirlwindBalls = 4;
				whirlwindAmount = 15;
				whirlSine = new Vector2(36, 3f);
				whirlSineFreq = 2.4f;
				idleSine = new Vector2(12f, 12f);
				idleSineFreq = 0.68f;
				extraBalls = 5;
				break;
			case Phase.Cracked:
				slamWait = 0.12f;
				slamDuration = 0.3f;
				whirlwindBalls = 5;
				whirlwindAmount = 15;
				whirlSine = new Vector2(40, 3f);
				whirlSineFreq = 4f;
				idleSine = new Vector2(12f, 12f);
				idleSineFreq = 1.1f;
				extraBalls = 9;
				break;
			case Phase.Shattering:
				slamWait = 0.1f;
				slamDuration = 0.2f;
				whirlwindBalls = 8;
				whirlwindAmount = 8;
				whirlSine = new Vector2(56f, 4f);
				whirlSineFreq = 6.7f;
				idleSine = new Vector2(3, 3);
				idleSineFreq = 6;
				extraBalls = 12;
				break;
		}
		UpdatePosition();

	}
	public void SetCollidable(bool value)
	{
		Player player = Scene.Tracker.GetEntity<Player>();
		if (player == null || player.Dead || !player.Active) return;
		Collidable = !fightDone && player.StateMachine.State != Player.StTempleFall && value;
	}

	public IEnumerator KeepSettingCollidableUntilTrue()
	{
		while (!Collidable)
		{
			SetCollidable(true);
			yield return null;
		}
		yield break;
	}
	public int attackCounter = 0;
	public Attacks attackType = Attacks.Idle;
	public IEnumerator Move(Vector2 target, Ease.Easer easing, float duration)
	{
		Tween tween = Tween.Create(Tween.TweenMode.Oneshot, easing, duration, start: true);
		tween.OnUpdate = (Tween t) =>
		{
			target.X = Calc.Max(Calc.Min(target.X,rightB.Position.X),leftB.Position.X);
			anchor = (Vector2.Lerp(anchor, target, t.Eased));
		};
		Add(tween);
		yield return duration;
	}
	public float introTextAlpha = 0f;
    public IEnumerator Pattern(Player player)
	{
		if (player == null || player.Dead || !player.Active || fightDone) yield break;
		if (arriving)
        {
            player.StateMachine.State = Player.StDummy;
			SceneAs<Level>().PauseLock = true;
            yield return 1f;
            yield return Move(center.Position, Ease.QuintInOut, 5f);
			yield return 1f;
            Audio.Play("event:/game/06_reflection/badeline_freakout_" + Random.Shared.Next(1, 3).ToString());
            SceneAs<Level>().Displacement.AddBurst(Position, 2.2f, 16f, 300f, 1f);
            yield return 1f;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.ExpoInOut, 1, start: true);
            tween.OnUpdate = (Tween t) =>
            {
                introTextAlpha = t.Eased;
            };
            Add(tween);
            yield return 5f;
            Tween tween2 = Tween.Create(Tween.TweenMode.Oneshot, Ease.ExpoInOut, 1, start: true);
            tween2.OnUpdate = (Tween t) =>
            {
                Vector2 renderPos = new Vector2(Celeste.ViewWidth, Celeste.ViewHeight - 64);
				introTextAlpha = Math.Abs(t.Eased - 1);
            };
            Add(tween2);
            SceneAs<Level>().PauseLock = false;
			arriving = false;
            Audio.SetMusic("event:/Prickles/OdysseyOfSand/heartboss");
            Audio.SetMusicParam("b_side", 0);
            SceneAs<Level>().Session.SetFlag("seen_arrival");
            player.StateMachine.State = Player.StNormal;
        }
		else if (patternCoroutine != null)
		{
			if (!beatdownCutscene)
				switch (attackType)
				{
					case Attacks.Idle:
						yield return IdleMovement();
						if (extraBalls > 0)
							for (int i = 0; i < extraBalls; i++)
							{
								ShootAtPlayer(player, 0.2f, Calc.ToRad(random.Next(-30,30)));
								yield return 0.09f;
							}
						attackCounter++;
						if (attackCounter >= 4)
						{
							attackType = Attacks.Slam;
							attackCounter = 0;
						}
						break;
					case Attacks.Slam:
						yield return GroundSlam(player);
						attackCounter++;
						if (attackCounter >= 3)
						{
							attackType = Attacks.Whirlwind;
							attackCounter = 0;
						}
						break;
					case Attacks.Whirlwind:
						yield return IdleMovement();
						yield return Whirlwind(player);
						attackCounter++;
						if (attackCounter >= 1)
						{
							yield return SetSine(idleSine.X, idleSine.Y, idleSineFreq);
							SetWind(WindController.Patterns.None);
							SetWind(WindController.Patterns.Up);
							attackType = Attacks.Idle;
							attackCounter = 0;
						}
						break;
				}
			if (health <= ChipPhaseThreshold && currentPhase == Phase.Intact && !beatdownCutscene)
			{
				yield return StartBeatdown(player, Phase.Chipped);
			}
			if (health <= CrackPhaseThreshold && currentPhase == Phase.Chipped && !beatdownCutscene)
			{
				yield return StartBeatdown(player, Phase.Cracked);
			}
			if (health <= ShatterPhaseThreshold && currentPhase == Phase.Cracked && !beatdownCutscene)
			{
				yield return StartBeatdown(player, Phase.Shattering);
			}
			if (health <= 1 && currentPhase == Phase.Shattering && !beatdownCutscene)
			{
				yield return StartBeatdown(player, Phase.LastHit);
			}
		}
		patternCoroutine.Cancel();
	}
	public IEnumerator StartBeatdown(Player player, Phase nextPhase)
	{
		Show();
		foreach (HeartShot shot in SceneAs<Level>().Tracker.GetEntities<HeartShot>())
		{
			shot.RemoveSelf();
		}
		SceneAs<Level>().Add(new BeatdownCutscene(player, this, IsPastPhase(Phase.Shattering)));
		while (SceneAs<Level>().InCutscene)
		{
			yield return null;
		}
		beatdownHitCount = 10;
		currentPhase = nextPhase;
	}
	public IEnumerator IdleMovement()
	{
		wiggle = true;
		Show();
		Vector2 pos = nodes[random.Next(3)].Position;
		do pos = nodes[random.Next(3)].Position;
		while (Vector2.Distance(anchor, pos) <= 32);
		yield return Move(pos, Ease.CubeInOut,2f);
		yield return 0.2f;
		yield return null;
	}
	public IEnumerator GroundSlam(Player player)
	{
		wiggle = false;
		Hide();
		yield return 0.4f;
		sprite.Rate = 7;
		Audio.Play("event:/game/09_core/frontdoor_heartfill", Position);
		Show();
		anchor = new Vector2(player.Position.X, center.Y - 32);
		SceneAs<Level>().Displacement.AddBurst(Position, 1.5f, 16f, 300f, 1f);
		Vector2 pos = new Vector2(anchor.X, ground.Y);

		yield return slamWait;
		if (Math.Abs(anchor.X - player.Position.X) >= 24)
		{
			Audio.Play("event:/game/general/strawberry_laugh", Position).setPitch(1.2f);
			Vector2 tempPos = new Vector2(player.Position.X + 48 * -Math.Sign(anchor.X - player.Position.X), center.Y - 32);
			yield return Move(tempPos, Ease.QuintInOut, 0.1f);
			SceneAs<Level>().Displacement.AddBurst(Position, 2.35f, 4f, 96f, 0.75f);
			pos = new Vector2(anchor.X, ground.Y);
			yield return 0.05f;
		}
		yield return Move(pos, Ease.ExpoIn, slamDuration);
		Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
		for (int i = -8; (float)i < 8; i++)
		{
			for (int j = 0; (float)j < 5; j++)
			{
				base.Scene.Add(Engine.Pooler.Create<Debris>().Init(Position + new Vector2(4 + i * 4, 4 + j * -4), '1', true).BlastFrom(Position + Vector2.UnitY * 32));
			}
		}
		//Audio.Play("event:/new_content/game/10_farewell/puffer_splode", Position);
		Audio.Play("event:/game/general/wall_break_stone", Position);
		if (Vector2.Distance(anchor, player.Position) <= 32)
		{
			Celeste.Freeze(0.1f);
			player.ExplodeLaunch(Position,false, false);
			playerHP.Hurt(player, 31);
		}
		SceneAs<Level>().Displacement.AddBurst(Position, 3f, 24f, 144f, 1f);
		yield return null;
		anchor = center.Position;
		Hide();
		sprite.Rate = 2;
		yield return null;
	}
	public IEnumerator Whirlwind(Player player)
	{
		yield return SetSine(whirlSine.X,whirlSine.Y, whirlSineFreq);
		Input.Rumble(RumbleStrength.Strong, RumbleLength.TwoSeconds);
		Audio.Play("event:/game/06_reflection/scaryhair_whoosh", Position);
		SceneAs<Level>().Displacement.AddBurst(Position, 5f, 96f, 400, 1f);
		SetWind(random.Next(2) == 0 ? WindController.Patterns.LeftStrong : WindController.Patterns.RightStrong);

		for (int i = 0; i < whirlwindAmount; i++)
		{
			Audio.Play("event:/game/04_cliffside/snowball_spawn",Position);
			for (int j = 0; j < whirlwindBalls; j++)
			{
				ShootAtPlayer(player, 1.5f, IsPastPhase(Phase.Shattering) ? Calc.ToRad(random.Next(-45,45)) : 0);
				yield return 0.08f;
			}
			yield return 0.3f;
		}
		if (IsPastPhase(Phase.Chipped)) yield return GroundSlam(player);
		if (IsPastPhase(Phase.Cracked)) yield return GroundSlam(player);
	}
	public IEnumerator SetSine(float x, float y, float freq = 0.44f)
	{
		Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, 0.4f, start: true);
		tween.OnUpdate = (Tween t) =>
		{
			sineXMult = (MathHelper.Lerp(sineXMult, x, t.Eased));
			sineYMult = (MathHelper.Lerp(sineYMult, y, t.Eased));
			sine.Frequency = (MathHelper.Lerp(sine.Frequency, freq, t.Eased));
		};
		Add(tween);
		yield return 0.4f;
	}
	public void SetWind(WindController.Patterns pattern)
	{

		WindController windController = base.Scene.Entities.FindFirst<WindController>();
		if (windController == null)
		{
			windController = new WindController(pattern);
			base.Scene.Add(windController);
		}
		else
		{
			windController.SetPattern(pattern);
		}
	}
	public void ShootAtPlayer(Player player, float speed = 1, float radiansOffset = 0)
	{
		if (player != null)
		{
			Audio.Play(random.Next(2) == 0 ? "event:/char/madeline/dash_pink_right" : "event:/char/madeline/dash_pink_left", Position).setPitch(1.5f);
			SceneAs<Level>().Add(Engine.Pooler.Create<HeartShot>().Init(playerHP, this, player, speed, radiansOffset));
		}
	}
	public void Hide()
	{
		Visible = false;
		SetCollidable(false);
		bloom.Visible = false;
		light.Visible = false;
	}
	public void Show()
	{
		Visible = true;
		SetCollidable(true);
		bloom.Visible = true;
		light.Visible = true;
	}
	public new void OnPlayer(Player player)
	{
		if (fightDone || collected || (base.Scene as Level).Frozen || player.StateMachine.State == Player.StTempleFall)
		{
			return;
		}

		if (player.DashAttacking)
		{
			if (flashCor != null && !flashCor.Active)
			{
				flashCor.Replace(HeartFlash());
				flashCor.UseRawDeltaTime = true;
			}
			int damage = 1;
			switch (currentPhase)
			{
				case Phase.Chipped: damage = 35 + random.Next(1,8); break;
				case Phase.Cracked: damage = 80 + random.Next(1, 16); break;
				case Phase.Shattering: damage = 140 + random.Next(1, 24); break;
				default: damage = 1 + random.Next(3, 5); break;
			}
			if (!IsPastPhase(Phase.LastHit))
				health = Math.Max(1, health - damage);
			Audio.Play("event:/game/06_reflection/badeline_freakout_" + Random.Shared.Next(1, 3).ToString());
			Celeste.Freeze(0.05f);
		}
		else if (attackType != Attacks.Slam && !beatdownCutscene)
		{
			Celeste.Freeze(0.1f);
			playerHP.Hurt(player, 17);
		}


		if (!beatdownCutscene) {
			player.PointBounce(base.Center);
			if (bounceSfxDelay <= 0f)
			{
				Audio.Play("event:/game/general/crystalheart_bounce", Position);

				bounceSfxDelay = 0.1f;
			}
		}
		else
		{
			CrystalDebris.Burst(Position, sprite.Color, false, Math.Min(1,(int)currentPhase) * 2);
			Audio.Play("event:/game/07_summit/gem_get", Position).setPitch(1 + ((beatdownHitCount - 10) * -0.15f));
            SetCollidable(false);
			int orbCount = Math.Min(1, (int)currentPhase) * 2 + random.Next(3);
			for (int i = 0; i < orbCount; i++)
			{
				Scene.Add(new AbsorbOrb(Position));
			}
		}
		moveWiggler.Start();
		ScaleWiggler.Start();
		moveWiggleDir = (Center - player.Center).SafeNormalize(Vector2.UnitY);
		Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
	}
	public IEnumerator HeartFlash()
	{

		bloom.Radius = 64;
		yield return Engine.DeltaTime * 2;
        bloom.Radius = 16;
        flashCor.Cancel();
	}
	public IEnumerator MovePlayerTo(Player player, Vector2 target, Ease.Easer easing, float duration)
	{
		Tween tween = Tween.Create(Tween.TweenMode.Oneshot, easing, duration, start: true);
		tween.OnUpdate = (Tween t) =>
		{
			player.Position = (Vector2.Lerp(player.Position, target, t.Eased));
		};
		player.Add(tween);
		yield return duration;
	}

	public IEnumerator ColorgradeGlitch()
	{
		Level level = SceneAs<Level>();
		level.NextColorGrade("Prickles/OdysseyOfSand/timeriftintro",1);
		yield return 2f;
        level.NextColorGrade("panicattack",1);
    }
	
}

public class BeatdownCutscene : CutsceneEntity
{
	public bool finale;
	public SandstormHeartBoss boss;
	public Player player;
	public float tempMfall;
	public Coroutine tooLongCoroutine;
	public Coroutine limitCoroutine;
	public BeatdownCutscene(Player plr, SandstormHeartBoss b, bool final = false)
	{
		player = plr;
		if (player != null) tempMfall = player.maxFall;
		boss = b;
		finale = final;
	}
	public override void OnBegin(Level level)
	{
		Add(tooLongCoroutine = new Coroutine());
		tooLongCoroutine.UseRawDeltaTime = true;
		Add(new Coroutine(CutsceneBeatUp(player, finale)));
	}
	public override void OnEnd(Level level)
	{
		if (WasSkipped)
		{
			player.OverrideDashDirection = null;
			boss.timeWarper.Multiplier = 1;
			player.StateMachine.Locked = false;
			player.StateMachine.State = Player.StTempleFall;
			player.DummyFriction = true;
			player.DummyGravity = true;
			level.ResetZoom();
            if (finale)
            {
                boss.playerHP.Visible = false;
                boss.fightDone = true;
                boss.Collect(player);
                return;
            }
        }
		tooLongCoroutine.Cancel();
        boss.Add(new Coroutine(boss.KeepSettingCollidableUntilTrue()));
        if (boss.currentPhase == SandstormHeartBoss.Phase.Cracked)
        {
			boss.Add(new Coroutine(boss.ColorgradeGlitch()));
			Audio.SetMusicParam("b_side", 1);
		}
		Level.PauseLock = false;
		boss.beatdownCutscene = false;
		boss.wiggle = true;
		boss.attackCounter = 0;
		boss.attackType = SandstormHeartBoss.Attacks.Idle;
		player.maxFall = tempMfall;
		player.Depth = 0;
		boss.Depth = 0;
		MInput.Disabled = false;
		SaveData.Instance.Assists.DashAssist = wasDashAssistOn;
		boss.SetWind(WindController.Patterns.Up);
		Level.FormationBackdrop.Display = false;
		Level.FormationBackdrop.Alpha = 0f;
	}
	public IEnumerator Limit()
	{
		yield return 15f;
		EndCutscene(Level);
	}
	public IEnumerator CutsceneBeatUp(Player player, bool final = false)
    {
        Level.PauseLock = true;
        boss.SetCollidable(false);
		boss.wiggle = false;
		Level.Flash(Color.White);
		Level.FormationBackdrop.Display = true;
		Level.FormationBackdrop.Alpha = 1f;
		boss.Depth = -2000000;
		player.Depth = -2000000;
		tooLongCoroutine.Replace(YourTakingTooLong());
		boss.beatdownCutscene = true;
		player.StateMachine.Locked = true;
		boss.SetWind(WindController.Patterns.None);
		//boss.Add(new Coroutine(boss.SceneAs<Level>().ZoomTo(boss.SceneAs<Level>().WorldToScreen(boss.anchor) / 4, 1.2f, 2f)));
		player.StateMachine.State = Player.StDummy;
		player.DummyFriction = false;
		player.DummyGravity = false;
		player.Speed.Y = 0;
		player.maxFall = 0;
		player.Facing = (Facings)(-boss.beatdownCutsceneDir);
		yield return boss.MovePlayerTo(player, boss.anchor + boss.beatdownCutsceneDir * new Vector2(24, -24), Ease.SineInOut, 1f);
		boss.SetCollidable(true);
		boss.timeWarper.Multiplier = 0.1f;
		while (true)
		{
			player.DummyGravity = false;
			player.Speed.Y = 0;
			player.maxFall = 0;
			while (!Input.Dash.Pressed)
			{
				player.DummyGravity = false;
				player.Speed.X = 0;
				player.Speed.Y = 0;
				player.maxFall = 0;
				yield return null;
			}
			if (limitCoroutine != null && !limitCoroutine.Active && !final)
			{
				limitCoroutine.Replace(Limit());
			}
			boss.timeWarper.Multiplier = 1;
			yield return CutsceneDash(player, boss.beatdownCutsceneDir * new Vector2(-1, 1));
			boss.beatdownCutsceneDir *= -1;
			boss.beatdownHitCount--;
			if (boss.beatdownHitCount <= 0)
			{
				boss.timeWarper.Multiplier = 1;
				//boss.Add(new Coroutine(boss.SceneAs<Level>().ZoomBack(0.5f)));
				player.OverrideDashDirection = null;
				player.StateMachine.Locked = false;
				player.StateMachine.State = Player.StTempleFall;
				player.DummyFriction = true;
				player.DummyGravity = true;
				EndCutscene(Level, true);
                if (finale)
                {
                    boss.playerHP.Visible = false;
                    boss.fightDone = true;
                    boss.Collect(player);
                }
                yield break;
			}
			yield return boss.MovePlayerTo(player, boss.anchor + boss.beatdownCutsceneDir * new Vector2(24, -24), Ease.QuintInOut, 0.2f);

			boss.SetCollidable(true);
			boss.timeWarper.Multiplier = 0.1f;
		}

	}
	public class TooLongTutorial : Entity
	{
		public float opacity = 0f;
		public Coroutine opacCor;
		public bool disappearing = false;
		public TooLongTutorial()
		{
			Tag = Tags.HUD;
			opacCor = new Coroutine(TextOpacity(1, Ease.CubeInOut, 2f), false);
			opacCor.UseRawDeltaTime = true;
			Add(opacCor);
		}
		public void Disappear()
		{
			opacCor.Replace(TextOpacity(0, Ease.CubeInOut, 0.5f, true));
		}
		public override void Update()
		{
			base.Update();
			if (Input.Dash.Pressed && !disappearing)
			{
				disappearing = true;
				opacCor.Cancel();
				Disappear();
			}
		}
		public IEnumerator TextOpacity(float opac, Ease.Easer easing, float duration, bool kys = false)
		{
			Tween tween = Tween.Create(Tween.TweenMode.Oneshot, easing, duration, start: true);
			tween.OnUpdate = (Tween t) =>
			{
				opacity = (MathHelper.Lerp(opacity, opac, t.Eased));
			};
			Add(tween);
			yield return duration;
			if (kys) RemoveSelf();
			opacCor.Cancel();
		}
		public override void Render()
		{
			Vector2 renderPos = new Vector2(Celeste.ViewWidth / 2, Celeste.ViewHeight - 64);
			string text = $"Dash!";
			if (!Scene.Paused)
			{
				MTexture mTexture = Input.GuiButton(ButtonPromptToVirtualButton(ButtonPrompt.Dash),Input.PrefixMode.Latest);
				mTexture.Draw(renderPos - new Vector2(mTexture.Width * 1.25f,0), new Vector2(0f, mTexture.Height / 2), Color.White * opacity, new Vector2(1, 1f));
				ActiveFont.DrawOutline(text, renderPos + new Vector2(ActiveFont.Measure(text).X / 2,0), Vector2.One / 2, Vector2.One, Color.White * opacity, 2, Color.Black * opacity);
			}
			base.Render();
		}
	}
	public IEnumerator YourTakingTooLong()
	{
		yield return 3f;
		Scene.Add(new TooLongTutorial());
	}
	public bool wasDashAssistOn = false;
	public IEnumerator CutsceneDash(Player player, Vector2 dashDirection)
	{
		wasDashAssistOn = SaveData.Instance.Assists.DashAssist;
		SaveData.Instance.Assists.DashAssist = false;
		Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
		MInput.Disabled = true;
		player.OverrideDashDirection = dashDirection;
		player.StateMachine.Locked = false;
		player.StateMachine.State = player.StartDash();
		yield return 0.1f;
		player.Dashes = 2;
		//while (!player.OnGround() || player.Speed.Y < 0f)
		//{
		//    Input.MoveY.Value = (int)dashDirection.X;
		//    Input.MoveX.Value = (int)dashDirection.Y;
		//    yield return null;
		//}

		player.OverrideDashDirection = null;
		player.StateMachine.State = 11;
		player.StateMachine.Locked = true;
		MInput.Disabled = false;
		SaveData.Instance.Assists.DashAssist = wasDashAssistOn;
	}

}
