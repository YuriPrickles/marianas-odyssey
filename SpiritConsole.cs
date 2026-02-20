using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Mod.Entities;
using Celeste.Mod.OdysseyHelper;
using Celeste.Pico8;
using CelesteMod.Publicizer;
using IL.Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.OdysseyHelper;
[CustomEntity("OdysseyOfSand/SpiritConsole")]
public class SpiritConsole : Entity
{
	
	public Image sprite;

	
	public TalkComponent talk;

	
	public bool talking;

	
	public SoundSource sfx;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public SpiritConsole(Vector2 position)
		: base(position)
	{
		base.Depth = 1000;
		AddTag(Tags.TransitionUpdate);
		AddTag(Tags.PauseUpdate);
		Add(sprite = new Image(GFX.Game["objects/pico8Console"]));
		sprite.JustifyOrigin(0.5f, 1f);
		Add(talk = new TalkComponent(new Rectangle(-12, -8, 24, 8), new Vector2(0f, -24f), OnInteract));
	}

	public SpiritConsole(EntityData data, Vector2 position)
		: this(data.Position + position)
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void Update()
    {
        Player entity = base.Scene.Tracker.GetEntity<Player>();
        base.Update();
		if (entity != null)
		{
			if (sfx == null)
			{
				if (entity.Y < base.Y + 16f)
				{
					Add(sfx = new SoundSource("event:/env/local/03_resort/pico8_machine"));
				}
			}
			if ((Scene as Level).Session.GetFlag("playing_zeldalike"))
			{
				talking = true;
				(Scene as Level).PauseLock = true;
				entity.StateMachine.State = 11;
			}
			else if ((Scene as Level).Session.GetFlag("zeldalike_finished") && talking)
			{
				talking = false;
				(Scene as Level).PauseLock = false;
				entity.StateMachine.State = 0;
			}
		}
	}
	public void OnInteract(Player player)
	{
		if (!talking)
        {
            (Scene as Level).Session.SetFlag("zeldalike_finished", false);
            talking = true;
            (base.Scene as Level).PauseLock = true;
			Add(new Coroutine(InteractRoutine(player)));
		}
	}
	ZeldalikeWrapper wrapper;

    public IEnumerator InteractRoutine(Player player)
	{
		player.StateMachine.State = 11;
		yield return player.DummyWalkToExact((int)X - 6);
		player.Facing = Facings.Right;
		yield return 0.5f;
		bool done = false;
        SpotlightWipe.FocusPoint = player.Position - (Scene as Level).Camera.Position + new Vector2(0f, -8f);
		new SpotlightWipe(Scene, wipeIn: false, () =>
		{
			done = true;
			wrapper = new ZeldalikeWrapper((Scene as Level),(Scene as Level).Session.GetFlag("instakill_odyssey_zeldalike"),SceneAs<Level>().Session.Area.Mode == AreaMode.BSide);
            (Scene as Level).Session.SetFlag("playing_zeldalike", true);
            Scene.Add(wrapper);
		});
		while (!done)
		{
			yield return null;
		}

		yield return 0.25f;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void SceneEnd(Scene scene)
	{
		if (sfx != null)
		{
			sfx.Stop();
			sfx.RemoveSelf();
			sfx = null;
		}

		base.SceneEnd(scene);
	}
}