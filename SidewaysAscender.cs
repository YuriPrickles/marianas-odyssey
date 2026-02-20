using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Mod.Entities;
using Celeste.Mod.OdysseyHelper.Cutscenes;
using CelesteMod.Publicizer;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;
using static Celeste.MoonGlitchBackgroundTrigger;

namespace Celeste.Mod.OdysseyHelper;

[CustomEntity("OdysseyOfSand/SidewaysAscender")]
public class SidewaysAscender : Entity
{
    public class Fader : Entity
    {
        public float Fade;


        public SidewaysAscender manager;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Fader(SidewaysAscender manager)
        {
            this.manager = manager;
            base.Depth = -1000010;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Render()
        {
            if (Fade > 0f)
            {
                Vector2 position = (base.Scene as Level).Camera.Position;
                Draw.Rect(position.X - 10f, position.Y - 10f, 340f, 200f, (manager.Dark ? Color.Black : Color.White) * Fade);
            }
        }
    }


    public const string BeginSwapFlag = "beginswap_";


    public const string BgSwapFlag = "bgswap_";

    public readonly bool Dark;

    public readonly bool Ch9Ending;


    public bool introLaunch;


    public int index;


    public string cutscene;


    public Level level;


    public float fade;


    public float scroll;


    public bool outTheTop;


    public Color background;


    public string ambience;

    public FinalBossStarfield johnStarfield;
    public SidewaysAscender(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        base.Tag = Tags.TransitionUpdate;
        base.Depth = 8900;
        index = data.Int("index");
        cutscene = data.Attr("cutscene");
        introLaunch = data.Bool("intro_launch");
        Dark = data.Bool("dark");
        Ch9Ending = cutscene.Equals("CH9_FREE_BIRD", StringComparison.InvariantCultureIgnoreCase);
        ambience = data.Attr("ambience");
        background = (Dark ? Color.Black : Calc.HexToColor("75a0ab"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Added(Scene scene)
    {
        base.Added(scene);
        level = base.Scene as Level;
        Add(new Coroutine(Routine()));
    }


    float backdropAlpha = 1f;

    public IEnumerator Routine()
    {
        johnStarfield = level.Background.Get<FinalBossStarfield>();
        Player player = Scene.Tracker.GetEntity<Player>();
        while (player == null || player.Dead || player.X > X)
        {
            player = Scene.Tracker.GetEntity<Player>();
            johnStarfield.Alpha = 0;
            OdysseyHelperModule.Instance.makeFinalBossFieldsInvisible = true;
            yield return null;
        }

        if (index == 9)
        {
            yield return 1.6f;
        }
        OdysseyHelperModule.Instance.makeFinalBossFieldsInvisible = false;
        for (float tween = 0f; tween < 1f; tween += Engine.DeltaTime / 0.4f)
        {
            johnStarfield.Alpha = (MathHelper.Lerp(0, 1f, tween));
            yield return null;
        }
        player.Sprite.Play("launch");
        player.Sprite.Rotation = Calc.ToRad(-90);
        player.Speed = Vector2.Zero;
        player.StateMachine.State = 11;
        player.DummyGravity = false;
        player.DummyAutoAnimate = false;
        if (!string.IsNullOrWhiteSpace(ambience))
        {
            if (ambience.Equals("null", StringComparison.InvariantCultureIgnoreCase))
            {
                Audio.SetAmbience(null);
            }
            else
            {
                Audio.SetAmbience(SFX.EventnameByHandle(ambience));
            }
        }

        if (introLaunch)
        {
            FadeSnapTo(1f);
            level.Camera.Position = player.Center + new Vector2(-160f, -90f);
            yield return 2.3f;
        }
        else
        {
            yield return FadeTo(1f, Dark ? 2f : 0.8f);
            if (Ch9Ending)
            {
                level.Add(new CS10_FreeBird());
                while (true)
                {
                    yield return null;
                }
            }

            if (!string.IsNullOrEmpty(cutscene))
            {
                yield return 0.25f;
                SidewaysAscendCutscene cs = new SidewaysAscendCutscene(index, cutscene, Dark);
                level.Add(cs);
                yield return null;
                while (cs.Running)
                {
                    yield return null;
                }
            }
            else
            {
                yield return 0.5f;
            }
        }

        level.CanRetry = false;
        player.Sprite.Play("launch");
        Audio.Play("event:/char/madeline/summit_flytonext", player.Position);
        yield return 0.25f;
        Vector2 from2 = player.Position;
        for (float p2 = 0f; p2 < 1f; p2 += Engine.DeltaTime / 1f)
        {
            player.Position = Vector2.Lerp(from2, from2 + new Vector2(60f, 0f), Ease.CubeInOut(p2)) + Calc.Random.ShakeVector();
            Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
            yield return null;
        }

        Fader fader = new Fader(this);
        Scene.Add(fader);
        if (ShouldRestorePlayerX())
        {
            //player.Y = from2.Y;
        }

        from2 = player.Position;
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
        for (float p2 = 0f; p2 < 1f; p2 += Engine.DeltaTime / 0.5f)
        {
            float x = player.X;
            player.Position = Vector2.Lerp(from2, from2 + new Vector2(-160, 0), Ease.SineIn(p2));
            if (p2 == 0f || Calc.OnInterval(player.X, x, 16f))
            {
                level.Add(Engine.Pooler.Create<SpeedRing>().Init(player.Center, new Vector2(-1f, 0f).Angle(), Color.White));
            }

            if (p2 >= 0.5f)
            {
                fader.Fade = (p2 - 0.5f) * 2f;
            }
            else
            {
                fader.Fade = 0f;
            }

            yield return null;
        }
        johnStarfield.Alpha = 0;
        OdysseyHelperModule.Instance.makeFinalBossFieldsInvisible = true;

        level.CanRetry = true;
        outTheTop = true;
        player.Sprite.Rotation = 0;
        player.SummitLaunch(player.X);
        player.DummyGravity = true;
        player.DummyAutoAnimate = true;
        player.X = level.Bounds.Left + 64;
        player.ExplodeLaunch(player.Position + new Vector2(12, 0), true);
        //level.Session.SetFlag("bgswap_" + index);
        level.NextTransitionDuration = 0.05f;
        if (introLaunch)
        {
            //level.Add(new HeightDisplay(-1));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Update()
    {
        scroll += Engine.DeltaTime * 240f;
        base.Update();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Render()
    {
        //Draw.Rect(level.Camera.X - 10f, level.Camera.Y - 10f, 340f, 200f, background * fade);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Removed(Scene scene)
    {
        FadeSnapTo(0f);
        level.Session.SetFlag("bgswap_" + index, setTo: false);
        level.Session.SetFlag("beginswap_" + index, setTo: false);
        if (outTheTop)
        {
            ScreenWipe.WipeColor = (Dark ? Color.Black : Color.White);
            if (introLaunch)
            {
                new MountainWipe(base.Scene, wipeIn: true);
            }
            else if (level.Session.Level.Contains("2"))
            {
                AreaData.Get("Prickles/OdysseyOfSand/4-TerracottaTowers").DoScreenWipe(base.Scene, wipeIn: true);
            }
            else if (level.Session.Level.Contains("3"))
            {
                AreaData.Get("Prickles/OdysseyOfSand/3-MirageGrove").DoScreenWipe(base.Scene, wipeIn: true);
            }
            else if (level.Session.Level.Contains("4"))
            {
                AreaData.Get("Prickles/OdysseyOfSand/2-SpiritTemple").DoScreenWipe(base.Scene, wipeIn: true);
            }
            else if (level.Session.Level.Contains("5"))
            {
                AreaData.Get("Prickles/OdysseyOfSand/1-TheEmptiness").DoScreenWipe(base.Scene, wipeIn: true);
            }
            ScreenWipe.WipeColor = Color.Black;
        }

        base.Removed(scene);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]

    public IEnumerator FadeTo(float target, float duration = 0.8f)
    {
        while ((fade = Calc.Approach(fade, target, Engine.DeltaTime / duration)) != target)
        {
            FadeSnapTo(fade);
            yield return null;
        }

        FadeSnapTo(target);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]

    public void FadeSnapTo(float target)
    {
        fade = target;
        SetSnowAlpha(1f - fade);
        SetBloom(fade * 0.1f);
        if (!Dark)
        {
            return;
        }

        foreach (Parallax item in level.Background.GetEach<Parallax>())
        {
            item.CameraOffset.Y -= 25f * target;
        }

        foreach (Parallax item2 in level.Foreground.GetEach<Parallax>())
        {
            item2.Alpha = 1f - fade;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]

    public void SetBloom(float add)
    {
        level.Bloom.Base = AreaData.Get(level).BloomBase + add;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]

    public void SetSnowAlpha(float value)
    {
        Snow snow = level.Foreground.Get<Snow>();
        if (snow != null)
        {
            snow.Alpha = value;
        }

        RainFG rainFG = level.Foreground.Get<RainFG>();
        if (rainFG != null)
        {
            rainFG.Alpha = value;
        }

        WindSnowFG windSnowFG = level.Foreground.Get<WindSnowFG>();
        if (windSnowFG != null)
        {
            windSnowFG.Alpha = value;
        }
    }


    public static float Mod(float x, float m)
    {
        return (x % m + m) % m;
    }

    private bool ShouldRestorePlayerX()
    {
        return (Engine.Scene as Level).Session.Area.GetLevelSet() != "Celeste";
    }
}