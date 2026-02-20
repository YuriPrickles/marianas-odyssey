using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using CelesteMod.Publicizer;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.OdysseyHelper.Cutscenes;

public class SidewaysAscendCutscene : CutsceneEntity
{

    public int index;


    public string cutscene;


    public BadelineDummy badeline;


    public Player player;


    public Vector2 origin;


    public bool spinning;


    public bool dark;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public SidewaysAscendCutscene(int index, string cutscene, bool dark)
    {
        this.index = index;
        this.cutscene = cutscene;
        this.dark = dark;
    }

    public override void OnBegin(Level level)
    {
        Add(new Coroutine(Cutscene()));
    }


    public IEnumerator Cutscene()
    {
        while ((player = Scene.Tracker.GetEntity<Player>()) == null)
        {
            yield return null;
        }

        origin = player.Position;
        Audio.Play("event:/char/badeline/maddy_split");
        player.CreateSplitParticles();
        Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
        Level.Displacement.AddBurst(player.Position, 0.4f, 8f, 32f, 0.5f);
        player.Dashes = 1;
        player.Facing = Facings.Left;
        Scene.Add(badeline = new BadelineDummy(player.Position));
        badeline.AutoAnimator.Enabled = false;
        spinning = true;
        Add(new Coroutine(SpinCharacters()));
        yield return Textbox.Say(cutscene);
        Audio.Play("event:/char/badeline/maddy_join");
        spinning = false;
        yield return 0.25f;
        badeline.RemoveSelf();
        player.Dashes = 2;
        player.CreateSplitParticles();
        Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
        Level.Displacement.AddBurst(player.Position, 0.4f, 8f, 32f, 0.5f);
        EndCutscene(Level);
    }


    public IEnumerator SpinCharacters()
    {
        float dist = 0f;
        Vector2 center = player.Position;
        float timer = MathF.PI / 2f;
        badeline.Sprite.Play("fallslow");
        badeline.Sprite.Scale.X = -1f;
        while (spinning || dist > 0f)
        {
            dist = Calc.Approach(dist, spinning ? 1f : 0f, Engine.DeltaTime * 4f);
            int num = (int)(timer / (MathF.PI * 2f) * 14f + 10f);
            float num2 = (float)Math.Sin(timer);
            float num3 = (float)Math.Cos(timer);
            float num4 = Ease.CubeOut(dist) * 12f;
            player.Position = center - new Vector2(num2 * dist * 8f, num3 * num4);
            badeline.Position = center + new Vector2(num2 * dist * 8f, num3 * num4);
            timer -= Engine.DeltaTime * 2f;
            if (timer <= 0f)
            {
                timer += MathF.PI * 2f;
            }

            yield return null;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void OnEnd(Level level)
    {
        if (badeline != null)
        {
            badeline.RemoveSelf();
        }

        if (player != null)
        {
            player.Dashes = 2;
            player.Position = origin;
        }

        if (!dark)
        {
            //level.Add(new HeightDisplay(index));
        }
    }
}