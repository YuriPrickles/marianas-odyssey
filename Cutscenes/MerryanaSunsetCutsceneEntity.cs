using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Mod.Entities;
using CelesteMod.Publicizer;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;

namespace Celeste.Mod.OdysseyHelper.Cutscenes;
[CustomEntity("OdysseyOfSand/MerryanaSunsetCutsceneEntity")]
public class MerryanaSunsetCutsceneEntity : NPC
{
    public class Orb : Entity
    {
        public Image PlayerSprite;

        public BloomPoint Bloom;

    
        public float ease;

        public Vector2 Target;

        public Coroutine Routine;

        public float Ease
        {
            get
            {
                return ease;
            }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set
            {
                ease = value;
                PlayerSprite.Scale = Vector2.One * ease;
                Bloom.Alpha = ease;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Orb(Vector2 position)
            : base(position)
        {
            Add(PlayerSprite = new Image(GFX.Game["characters/badeline/orb"]));
            Add(Bloom = new BloomPoint(0f, 32f));
            Add(Routine = new Coroutine(FloatRoutine()));
            PlayerSprite.CenterOrigin();
            base.Depth = -10001;
        }

        public IEnumerator FloatRoutine()
        {
            Vector2 speed = Vector2.Zero;
            Ease = 0.2f;
            while (true)
            {
                Vector2 target = Target + Calc.AngleToVector(Calc.Random.NextFloat(MathF.PI * 2f), 16f + Calc.Random.NextFloat(40f));
                float reset = 0f;
                while (reset < 1f && (target - Position).Length() > 8f)
                {
                    Vector2 vector = (target - Position).SafeNormalize();
                    speed += vector * 420f * Engine.DeltaTime;
                    if (speed.Length() > 90f)
                    {
                        speed = speed.SafeNormalize(90f);
                    }

                    Position += speed * Engine.DeltaTime;
                    reset += Engine.DeltaTime;
                    Ease = Calc.Approach(Ease, 1f, Engine.DeltaTime * 4f);
                    yield return null;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public IEnumerator CircleRoutine(float offset)
        {
            Vector2 from = Position;
            float ease = 0f;
            Player player = Scene.Tracker.GetEntity<Player>();
            while (player != null)
            {
                float angleRadians = Scene.TimeActive * 2f + offset;
                Vector2 vector = player.Center + Calc.AngleToVector(angleRadians, 24f);
                ease = Calc.Approach(ease, 1f, Engine.DeltaTime * 2f);
                Position = from + (vector - from) * Monocle.Ease.CubeInOut(ease);
                yield return null;
            }
        }

        public IEnumerator AbsorbRoutine()
        {
            Player entity = Scene.Tracker.GetEntity<Player>();
            if (entity != null)
            {
                Vector2 from = Position;
                Vector2 to = entity.Center;
                for (float p = 0f; p < 1f; p += Engine.DeltaTime)
                {
                    float num = Monocle.Ease.BigBackIn(p);
                    Position = from + (to - from) * num;
                    Ease = 0.2f + (1f - num) * 0.8f;
                    yield return null;
                }
            }
        }
    }


    public bool started;


    public Image white;


    public BloomPoint bloom;


    public VertexLight light;

    public SoundSource LoopingSfx;


    public List<Orb> orbs = new List<Orb>();
    public PlayerHair Hair;
    public PlayerSprite PlayerSprite;
    public MerryanaSunsetCutsceneEntity(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        PlayerSprite = new PlayerSprite(PlayerSpriteMode.Badeline);
        Add(Hair = new PlayerHair(PlayerSprite));
        Add(PlayerSprite);
        Hair.Color = Color.White;
        PlayerSprite.Play("asleep");
        Add(white = new Image(GFX.Game["characters/Prickles/Merryana/playerSprite/white00"]));
        white.Color = Color.White * 0f;
        white.Origin = PlayerSprite.Origin;
        white.Position = PlayerSprite.Position;
        Add(bloom = new BloomPoint(new Vector2(0f, -6f), 0f, 16f));
        Add(light = new VertexLight(new Vector2(0f, -6f), Color.White, 1f, 24, 64));
        Add(LoopingSfx = new SoundSource("event:/none"));
        MoveAnim = "walk";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        if (!base.Session.GetFlag("badeline_connection"))
        {
            return;
        }

        RemoveSelf();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Update()
    {
        base.Update();
        Player entity = base.Scene.Tracker.GetEntity<Player>();
        if (!started && entity != null && entity.X > base.X - 32f)
        {
            base.Scene.Add(new LoveCutscene(entity, this));
            started = true;
        }
        if (PlayerSprite.CurrentAnimationID == "hug")
        {

        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        foreach (Orb orb in orbs)
        {
            orb.RemoveSelf();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public IEnumerator TurnWhite(float duration)
    {
        white.Scale.X = -1;
        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha += Engine.DeltaTime / duration;
            white.Color = Color.White * alpha;
            bloom.Alpha = alpha;
            yield return null;
        }
        Hair.Visible = false;
        PlayerSprite.Visible = false;
    }

    public IEnumerator Disperse()
    {
        Input.Rumble(RumbleStrength.Light, RumbleLength.Long);
        float size = 1f;
        while (orbs.Count < 8)
        {
            float to = size - 0.125f;
            while (size > to)
            {
                white.Scale = Vector2.One * size;
                light.Alpha = size;
                bloom.Alpha = size;
                size -= Engine.DeltaTime;
                yield return null;
            }

            Orb orb = new Orb(Position);
            orb.Target = Position + new Vector2(-16f, -40f);
            Scene.Add(orb);
            orbs.Add(orb);
        }

        yield return 3.25f;
        int i = 0;
        foreach (Orb orb2 in orbs)
        {
            orb2.Routine.Replace(orb2.CircleRoutine((float)i / 8f * (MathF.PI * 2f)));
            i++;
            yield return 0.2f;
        }

        yield return 2f;
        foreach (Orb orb3 in orbs)
        {
            orb3.Routine.Replace(orb3.AbsorbRoutine());
        }

        yield return 1f;
    }
    public IEnumerator BetterMoveTo(Vector2 target, bool fadeIn = false, int? turnAtEndTo = null, bool removeAtEnd = false)
    {
        if (removeAtEnd)
        {
            Tag |= Tags.TransitionUpdate;
        }

        if (Math.Sign(target.X - X) != 0 && PlayerSprite != null)
        {
            PlayerSprite.Scale.X = Math.Sign(target.X - X);
        }

        (target - Position).SafeNormalize();
        float alpha = (fadeIn ? 0f : 1f);
        if (PlayerSprite != null && PlayerSprite.Has(MoveAnim))
        {
            PlayerSprite.Play(MoveAnim);
        }

        float speed = 0f;
        while ((MoveY && Position != target) || (!MoveY && X != target.X))
        {
            speed = Calc.Approach(speed, Maxspeed, 160f * Engine.DeltaTime);
            if (MoveY)
            {
                Position = Calc.Approach(Position, target, speed * Engine.DeltaTime);
            }
            else
            {
                X = Calc.Approach(X, target.X, speed * Engine.DeltaTime);
            }

            if (PlayerSprite != null)
            {
                PlayerSprite.Color = Color.White * alpha;
            }

            alpha = Calc.Approach(alpha, 1f, Engine.DeltaTime);
            yield return null;
        }

        if (PlayerSprite != null && PlayerSprite.Has(IdleAnim))
        {
            PlayerSprite.Play(IdleAnim);
        }

        while (alpha < 1f)
        {
            if (PlayerSprite != null)
            {
                PlayerSprite.Color = Color.White * alpha;
            }

            alpha = Calc.Approach(alpha, 1f, Engine.DeltaTime);
            yield return null;
        }

        if (turnAtEndTo.HasValue && PlayerSprite != null)
        {
            PlayerSprite.Scale.X = turnAtEndTo.Value;
        }

        if (removeAtEnd)
        {
            Scene.Remove(this);
        }

        yield return null;
    }
}