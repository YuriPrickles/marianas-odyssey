using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Mod.OdysseyHelper;
using CelesteMod.Publicizer;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.OdysseyHelper;

[Pooled]
[Tracked(false)]
public class HeartShot : Entity
{
	public enum ShotPatterns
	{
		Single,
		Double,
		Triple
	}

	public const float MoveSpeed = 100f;
	public const float CantKillTime = 0.15f;
	public const float AppearTime = 0.1f;
	public SandstormHeartBoss boss;
	public Level level;
	public Vector2 speed;
	public float speedMult;
	public float particleDir;
	public Vector2 anchor;
	public Vector2 perp;
	public Player target;
	public Vector2 targetPt;
	public float angleOffset;
	public bool dead;
	public float cantKillTimer;
	public float appearTimer;
	public bool hasBeenInCamera;
	public SineWave sine;
	public float sineMult;

	public Sprite sprite;
	public HPController playerHP;
	public HeartShot()
		: base(Vector2.Zero)
	{
		Add(sprite = GFX.SpriteBank.Create("badeline_projectile"));
		base.Collider = new Hitbox(4f, 4f, -2f, -2f);
		Add(new PlayerCollider(OnPlayer));
		base.Depth = -1000000;
		Add(sine = new SineWave(1.4f, 0f));
	}
	public HeartShot Init(HPController hpc, SandstormHeartBoss boss, Player target, float _speedMult = 1f, float angleOffset = 0f)
    {
		speedMult = _speedMult;
        playerHP = hpc;
        this.boss = boss;
		anchor = (Position = boss.Center);
		this.target = target;
		this.angleOffset = angleOffset;
		dead = (hasBeenInCamera = false);
		cantKillTimer = 0.15f;
		appearTimer = 0.1f;
		sine.Reset();
		sineMult = 0f;
		sprite.Play("charge", restart: true);
		InitSpeed();
		return this;
	}
	public HeartShot Init(SandstormHeartBoss boss, Vector2 target)
	{
		this.boss = boss;
		anchor = (Position = boss.Center);
		this.target = null;
		angleOffset = 0f;
		targetPt = target;
		dead = (hasBeenInCamera = false);
		cantKillTimer = 0.15f;
		appearTimer = 0.1f;
		sine.Reset();
		sineMult = 0f;
		sprite.Play("charge", restart: true);
		InitSpeed();
		return this;
	}

	public void InitSpeed()
	{
		if (target != null)
		{
			speed = (target.Center - base.Center).SafeNormalize(100f) * speedMult;
		}
		else
		{
			speed = (targetPt - base.Center).SafeNormalize(100f) * speedMult;
		}

		if (angleOffset != 0f)
		{
			speed = speed.Rotate(angleOffset);
		}

		perp = speed.Perpendicular().SafeNormalize();
		particleDir = (-speed).Angle();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Added(Scene scene)
	{
		base.Added(scene);
		level = SceneAs<Level>();
	}

	public override void Removed(Scene scene)
	{
		base.Removed(scene);
		level = null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Update()
	{
		base.Update();
		if (appearTimer > 0f)
		{
			Position = (anchor = boss.Position);
			appearTimer -= Engine.DeltaTime;
			return;
		}

		if (cantKillTimer > 0f)
		{
			cantKillTimer -= Engine.DeltaTime;
		}

		anchor += speed * Engine.DeltaTime;
		Position = anchor + perp * sineMult * sine.Value * 3f;
		sineMult = Calc.Approach(sineMult, 1f, 2f * Engine.DeltaTime);
		if (!dead)
		{
			bool flag = level.IsInCamera(Position, 8f);
			if (flag && !hasBeenInCamera)
			{
				hasBeenInCamera = true;
			}
			else if (!flag && hasBeenInCamera)
			{
				Destroy();
			}

			if (base.Scene.OnInterval(0.04f))
			{
				//level.ParticlesFG.Emit(P_Trail, 1, base.Center, Vector2.One * 2f, particleDir);
			}
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Render()
	{
		Color color = sprite.Color;
		Vector2 position = sprite.Position;
		sprite.Color = Color.Black;
		sprite.Position = position + new Vector2(-1f, 0f);
		sprite.Render();
		sprite.Position = position + new Vector2(1f, 0f);
		sprite.Render();
		sprite.Position = position + new Vector2(0f, -1f);
		sprite.Render();
		sprite.Position = position + new Vector2(0f, 1f);
		sprite.Render();
		sprite.Color = color;
		sprite.Position = position;
		base.Render();
	}

	public void Destroy()
	{
		dead = true;
		RemoveSelf();
	}
	public void OnPlayer(Player player)
	{
		if (!dead)
		{
			if (cantKillTimer > 0f)
			{
				Destroy();
			}
			else
            {
                Audio.Play("event:/game/general/seed_poof", Position);
                Destroy();
                Celeste.Freeze(0.1f);
				playerHP.Hurt(player,9);
			}
		}
	}
}