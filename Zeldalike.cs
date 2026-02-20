using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Celeste.Mod;
using CelesteMod.Publicizer;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod;
using SimplexNoise;

namespace Celeste.Mod.OdysseyHelper
{
	public partial class Zeldalike
	{

		public StolenEmulator E;

		public Point room;

		public List<ClassicObject> objects;
		public int freeze;
		public int shake;
		public bool will_restart;
		public int delay_restart;
		public HashSet<int> got_fruit;
		public bool has_dashed;
		public int sfx_timer;
		public bool has_key;
		public bool pause_player;
		public bool flash_bg;
		public int music_timer;
		public bool new_bg;
		public int k_left;
		public int k_right = 1;
		public int k_up = 2;
		public int k_down = 3;
		public int k_jump = 4;
		public int k_dash = 5;
		public int frames;
		public int seconds;
		public int minutes;
		public int deaths;
		public int max_djump;
		public bool start_game;
		public int start_game_flash;
		public bool room_just_loaded;
		public bool stop_timekeeping = false;
		public class ClassicObject
		{
			public bool ignore_solids = false;
			public Zeldalike G;

			public StolenEmulator E;

			public int type;

			public bool collideable = true;

			public bool solids = true;

			public float spr;

			public bool flipX;

			public bool flipY;

			public float x;

			public float y;

			public Rectangle hitbox = new Rectangle(0, 0, 8, 8);

			public Vector2 spd = new Vector2(0f, 0f);

			public Vector2 rem = new Vector2(0f, 0f);

			public virtual void init(Zeldalike g, StolenEmulator e)
			{
				G = g;
				E = e;
			}

			public virtual void update()
			{
			}
			public virtual void on_destroy()
			{

			}

			public virtual void draw()
			{
				if (spr > 0f)
				{
					E.spr(spr, x, y, 1, 1, flipX, flipY);
				}
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public bool is_solid(int ox, int oy)
			{
				if (ignore_solids) return false;
				if (!G.solid_at(x + (float)hitbox.X + (float)ox, y + (float)hitbox.Y + (float)oy, hitbox.Width, hitbox.Height))
				{
					return check<cage_door>(ox,oy);
				}

				return true;
			}
			[MethodImpl(MethodImplOptions.NoInlining)]
			public T collide<T>(int ox, int oy) where T : ClassicObject
			{
				Type typeFromHandle = typeof(T);
				foreach (ClassicObject @object in G.objects)
				{
					if (@object != null && @object.GetType() == typeFromHandle && @object != this && @object.collideable && @object.x + (float)@object.hitbox.X + (float)@object.hitbox.Width > x + (float)hitbox.X + (float)ox && @object.y + (float)@object.hitbox.Y + (float)@object.hitbox.Height > y + (float)hitbox.Y + (float)oy && @object.x + (float)@object.hitbox.X < x + (float)hitbox.X + (float)hitbox.Width + (float)ox && @object.y + (float)@object.hitbox.Y < y + (float)hitbox.Y + (float)hitbox.Height + (float)oy)
					{
						return @object as T;
					}
				}

				return null;
			}

			public bool check<T>(int ox, int oy) where T : ClassicObject
			{
				return collide<T>(ox, oy) != null;
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public void move(float ox, float oy)
			{
				int num = 0;
				rem.X += ox;
				num = E.flr(rem.X + 0.5f);
				rem.X -= num;
				move_x(num, 0);
				rem.Y += oy;
				num = E.flr(rem.Y + 0.5f);
				rem.Y -= num;
				move_y(num);
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public void move_x(int amount, int start)
			{
				if (solids)
				{
					int num = G.sign(amount);
					for (int i = start; (float)i <= E.abs(amount); i++)
					{
						if (!is_solid(num, 0))
						{
							x += num;
							continue;
						}

						spd.X = 0f;
						rem.X = 0f;
						break;
					}
				}
				else
				{
					x += amount;
				}
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public void move_y(int amount)
			{
				if (solids)
				{
					int num = G.sign(amount);
					for (int i = 0; (float)i <= E.abs(amount); i++)
					{
						if (!is_solid(0, num))
						{
							y += num;
							continue;
						}

						spd.Y = 0f;
						rem.Y = 0f;
						break;
					}
				}
				else
				{
					y += amount;
				}
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public ClassicObject()
			{
			}
		}
		public class message : ClassicObject
		{
			public float last;

			public float index;

			[MethodImpl(MethodImplOptions.NoInlining)]
			public override void draw()
			{
				E.spr(20, x, y);
				string text = "you should have#been there#instead of her#you are to blame#for all that occurred";
				if (check<player>(4, 4))
				{
					if (index < (float)text.Length)
					{
						index += 0.5f;
						if (index >= last + 1f)
						{
							last += 1f;
							E.sfx(35);
						}
					}

					Vector2 vector = new Vector2(8f, 80f);
					for (int i = 0; (float)i < index; i++)
					{
						if (text[i] != '#')
						{
							E.rectfill(vector.X - 2f, vector.Y - 2f, vector.X + 7f, vector.Y + 6f, 7f);
							E.print(text[i].ToString() ?? "", vector.X, vector.Y, 0f);
							vector.X += 5f;
						}
						else
						{
							vector.X = 8f;
							vector.Y += 7f;
						}
					}
				}
				else
				{
					index = 0f;
					last = 0f;
				}
			}
		}
		public class crystal_heart : ClassicObject
		{
			float offset = 0;
			float cutscene_timer = 0;
			bool collected = false;
			bool pressed_key = false;
			int flash_timer;
			public override void update()
			{
				if (collected)
				{
					player p = find_player();
					if (p != null)
                    {
						p.spd = Vector2.Zero;
                        foreach (ClassicObject obj in G.objects)
                        {
							if ((obj != null && obj is enemy && obj != this))
							{
								obj.spr = 0;
								obj.hitbox = new Rectangle(0, 0, 0, 0);
								(obj as enemy).vulnerable = true;
                                (obj as enemy).health = 0;
                            }
						}
                        G.pause_player = true;
						cutscene_timer++;
						if (cutscene_timer == 120) E.sfx(14);
						if (cutscene_timer >=120)
						{

							if (pressed_key)
							{
								flash_timer--;
								if (flash_timer <= -30)
								{
									E.end_game(true);
								}
							}
							else if ((E.btn(G.k_jump) || E.btn(G.k_dash)))
                            {
                                pressed_key = true;

                                flash_timer = 50;
                                E.sfx(38);
                            }
                            
						}
					}
					return;
				}
				offset += 0.04f;
				y = 2f * E.sin(offset * 0.6f) + 16;
				hitbox = new Rectangle(-1, -1, 18, 18);
				player player = collide<player>(0, 0);
				if (player != null && player.dash_effect_time > 0)
				{
					player.spd.X = (float)(-G.sign(player.spd.X)) * 1.5f;
					player.spd.Y = -1.5f;
					player.dash_time = -1;
					E.sfx(37);
                    Audio.SetMusic(null);
                    collected = true;
					G.stop_timekeeping = true;
				}

				hitbox = new Rectangle(0, 0, 16, 16);
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public override void draw()
			{
				if (collected)
				{
					E.rectfill(x + 8 - cutscene_timer, y + 8 - cutscene_timer, x + 8 + cutscene_timer, y + 8 + cutscene_timer, 0);
				}
				float heart_topleft = E.b_side ? 44 : 96;
				E.spr(heart_topleft, x, y);
				E.spr(heart_topleft + 1f, x + 8f, y);
				E.spr(heart_topleft + 16f, x, y + 8f);
				E.spr(heart_topleft + 17, x + 8f, y + 8f);
				if (cutscene_timer >= 120)
				{
					E.print(E.b_side ? "tunnel vision" : "quarry of the mind", 32, 56, E.b_side ? 8 : 12);
					G.draw_time(32, 80);
					if (pressed_key)
					{
						E.pal();
						int num = 10;
						if (flash_timer <= 10)
						{
							num = ((flash_timer > 5) ? 2 : ((flash_timer > 0) ? 1 : 0));
						}
						else if (G.frames % 10 < 5)
						{
							num = 7;
						}

						if (num < 10)
						{
							for (int i = 1; i < 16; i++)
							{
								E.pal(i, num);
							}
						}
					}
				}
			}
			public player find_player()
			{
				foreach (var obj in G.objects)
				{
					if (obj is player)
					{
						return obj as player;
					}
				}
				return null;
			}
		}
		public class room_title : ClassicObject
		{
			public float delay = 5f;
			public override void draw()
			{
				int final = 14;
				float draw_end = (G.level_index() == final) ? -120f : -30f;

				delay -= 1f;
				if (delay < draw_end)
				{
					G.destroy_object(this);
				}
				else if (delay < 0f)
				{
					if (G.level_index() == final)
					{
						E.rectfill(24f, 58f, 104f, 78f, 0f);
						E.print("part of you", 40f, 62f, 7f);
						E.print("in crab form", 40f, 70f, 6f);
					}
					else
					{
						E.rectfill(24f, 58f, 104f, 70f, 0f);
						int num = (1 + G.level_index());
						E.print("floor " + num, 52, 62f, 7f);
					}

					G.draw_time(4, 4);
				}
			}
		}
		public class player_spawn : ClassicObject
		{
			public Vector2 target;
			public int state;
			public int delay;
			public player_hair hair;

			[MethodImpl(MethodImplOptions.NoInlining)]
			public override void init(Zeldalike g, StolenEmulator e)
			{
				base.init(g, e);
				spr = 3f;
				target = new Vector2(x, y);
				y = 128f;
				spd.Y = -4f;
				state = 0;
				delay = 0;
				solids = false;
				hair = new player_hair(this);
				E.sfx(4);
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public override void update()
			{
				if (state == 0)
				{
					if (y < target.Y + 16f)
					{
						state = 1;
						delay = 3;
					}
				}
				else if (state == 1)
				{
					spd.Y += 0.5f;
					if (spd.Y > 0f && delay > 0)
					{
						spd.Y = 0f;
						delay--;
					}

					if (spd.Y > 0f && y > target.Y)
					{
						y = target.Y;
						spd = new Vector2(0f, 0f);
						state = 2;
						delay = 5;
						G.shake = 5;
						//G.init_object(new smoke(), x, y + 4f);
						E.sfx(5);
					}
				}
				else if (state == 2)
				{
					delay--;
					spr = 3f;
					if (delay < 0)
					{
						G.destroy_object(this);
						G.init_object(new player(), x, y).hair = hair;
					}
				}
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public override void draw()
			{
				hair.draw_hair(this, 1, G.max_djump);
				G.draw_player(this, G.max_djump);
			}
		}
		public int sign(float v)
		{
			if (!(v > 0f))
			{
				if (!(v < 0f))
				{
					return 0;
				}

				return -1;
			}

			return 1;
		}

		public void title_screen()
		{
			got_fruit = new HashSet<int>();
			frames = 0;
			deaths = 0;
			max_djump = 1;
			start_game = false;
			start_game_flash = 0;
			E.music(0, 0, 7);
			load_room(7, 1);
		}

		public void begin_game()
		{
			frames = 0;
			seconds = 0;
			minutes = 0;
			music_timer = 0;
			start_game = false;
			E.music(0, 0, 7);
			load_room(0, 0);
		}
		public int level_index()
		{
			return room.X % 8 + room.Y * 8;
		}

		public bool is_title()
		{
			return level_index() == 15;
		}
		public void psfx(int num)
		{
			if (sfx_timer <= 0)
			{
				E.sfx(num);
			}
		}
		public void draw_player(ClassicObject obj, int djump)
		{
			int num = 0;
			switch (djump)
			{
				case 2:
					num = ((E.flr(frames / 3 % 2) != 0) ? 144 : 160);
					break;
				case 0:
					num = 128;
					break;
			}

			E.spr(obj.spr + (float)num, obj.x, obj.y, 1, 1, obj.flipX, obj.flipY);
		}
		public T init_object<T>(T obj, float x, float y, int? tile = null) where T : ClassicObject
		{
			objects.Add(obj);
			if (tile.HasValue)
			{
				obj.spr = tile.Value;
			}

			obj.x = (int)x;
			obj.y = (int)y;
			obj.init(this, E);
			return obj;
		}
		public void destroy_object(ClassicObject obj)
		{
			int num = objects.IndexOf(obj);
			if (num >= 0)
			{
				objects[num].on_destroy();
				objects[num] = null;
			}
		}
		public void kill_player(player obj)
		{
			sfx_timer = 12;
			E.sfx(0);
			deaths++;
			shake = 10;
			destroy_object(obj);
			Stats.Increment(Stat.PICO_DEATHS);
			dead_particles.Clear();
			for (int i = 0; i <= 7; i++)
			{
				float num = (float)i / 8f;
				dead_particles.Add(new DeadParticle
				{
					x = obj.x + 4f,
					y = obj.y + 4f,
					t = 10,
					spd = new Vector2(E.cos(num) * 3f, E.sin(num + 0.5f) * 3f)
				});
			}

			restart_room();
		}
		public void restart_room()
		{
			will_restart = true;
			delay_restart = 15;
		}
		public void next_room()
		{
			if (room.X == 7)
			{
				load_room(0, room.Y + 1);
			}
			else
			{
				load_room(room.X + 1, room.Y);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public void load_room(int x, int y)
		{
			room_just_loaded = true;
			has_dashed = false;
			has_key = false;
			for (int i = 0; i < objects.Count; i++)
			{
				objects[i] = null;
			}

			room.X = x;
			room.Y = y;
            if (room.X == 6 && room.Y == 1)
            {
                E.music(20, 500, 7);
            }
            else if (room.X == 5 && room.Y == 1)
            {
                E.music(30, 500, 7);
            }
            for (int j = 0; j <= 15; j++)
			{
				for (int k = 0; k <= 15; k++)
				{
					int num = E.mget(room.X * 16 + j, room.Y * 16 + k);
					switch (num)
					{
						case 11:
							//init_object(new platform(), j * 8, k * 8).dir = -1f;
							continue;
						case 12:
							//init_object(new platform(), j * 8, k * 8).dir = 1f;
							continue;
					}

					ClassicObject classicObject = null;
					switch (num)
					{
						case 1:
							classicObject = new player_spawn();
							break;
						case 15:
							classicObject = new spirit();
							break;
						case 20:
							classicObject = new message();
							break;
						case 41:
							classicObject = new zipmover();
							break;
						case 56:
							classicObject = new cage_door();
							break;
						case 64:
							classicObject = new merryana();
							break;
						case 72:
							classicObject = new spikes();
							break;
						case 88:
							classicObject = new spikes();
							(classicObject as spikes).permanent = true;
							break;
						case 96:
							classicObject = new crystal_heart();
							break;
						case 98:
							classicObject = new melvin();
							break;
					}

					if (classicObject != null)
					{
						init_object(classicObject, j * 8, k * 8, num);
					}
				}
			}

			if (!is_title())
			{
				init_object(new room_title(), 0f, 0f);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public void Update()
		{
			frames = (frames + 1) % 30;
			if (frames == 0 && level_index() < 15 && !stop_timekeeping)
			{
				seconds = (seconds + 1) % 60;
				if (seconds == 0)
				{
					minutes++;
				}
			}

			if (music_timer > 0)
			{
				music_timer--;
				if (music_timer <= 0)
				{
					E.music(10, 0, 7);
				}
			}

			if (sfx_timer > 0)
			{
				sfx_timer--;
			}

			if (freeze > 0)
			{
				freeze--;
				return;
			}

			if (shake > 0)
			{
				shake--;
				E.camera();
				if (shake > 0)
				{
					if (Settings.Instance.ScreenShake == ScreenshakeAmount.On)
					{
						E.camera(-2f + E.rnd(5f), -2f + E.rnd(5f));
					}
					else
					{
						E.camera(-1f + E.rnd(3f), -1f + E.rnd(3f));
					}
				}
			}

			if (will_restart && delay_restart > 0)
			{
				delay_restart--;
				if (delay_restart <= 0)
				{
					will_restart = true;
					load_room(room.X, room.Y);
				}
			}

			room_just_loaded = false;
			int num = 0;
			while (num != -1)
			{
				int i = num;
				num = -1;
				for (; i < objects.Count; i++)
				{
					ClassicObject classicObject = objects[i];
					if (classicObject != null)
					{
						classicObject.move(classicObject.spd.X, classicObject.spd.Y);
						classicObject.update();
						if (room_just_loaded)
						{
							room_just_loaded = false;
							num = i;
							break;
						}
					}
				}

				while (objects.IndexOf(null) >= 0)
				{
					objects.Remove(null);
				}
			}

			if (!is_title())
			{
				return;
			}

			if (!start_game && (E.btn(k_jump) || E.btn(k_dash)))
			{
				E.music(-1, 0, 0);
				start_game_flash = 50;
				start_game = true;
				E.sfx(38);
			}

			if (start_game)
			{
				start_game_flash--;
				if (start_game_flash <= -30)
				{
					begin_game();
				}
			}
		}

		public bool p_iframing = false;
		public void DrawMap()
        {
            int num2 = 9;
            if (flash_bg)
            {
                num2 = frames / 5;
            }
            else if (new_bg)
            {
                num2 = 0;
            }
            if (is_title()) num2 = 0;
            E.rectfill(0f, 0f, 128f, 128f, num2);
            E.map(room.X * 16, room.Y * 16, 0, 0, 16, 16, 4);
            E.map(room.X * 16, room.Y * 16, 0, 0, 16, 16, 2);
            int tx = (is_title() ? (-4) : 0);
            E.map(room.X * 16, room.Y * 16, tx, 0, 16, 16, 1);
            E.map(room.X * 16, room.Y * 16, 0, 0, 16, 16, 3);
        }
		public void Draw()
		{
			E.pal();
			if (start_game)
			{
				int num = 10;
				if (start_game_flash <= 10)
				{
					num = ((start_game_flash > 5) ? 2 : ((start_game_flash > 0) ? 1 : 0));
				}
				else if (frames % 10 < 5)
				{
					num = 7;
				}

				if (num < 10)
				{
					E.pal(11, num);
					E.pal(8, num);
					E.pal(10, num);
					E.pal(5, num);
					E.pal(6, num);
					E.pal(7, num);
				}
			}

			//if (!is_title())
			//{
			//    foreach (Cloud cloud in clouds)
			//    {
			//        cloud.x += cloud.spd;
			//        E.rectfill(cloud.x, cloud.y, cloud.x + cloud.w, cloud.y + 4f + (1f - cloud.w / 64f) * 12f, (!new_bg) ? 1 : 14);
			//        if (cloud.x > 128f)
			//        {
			//            cloud.x = 0f - cloud.w;
			//            cloud.y = E.rnd(120f);
			//        }
			//    }
			//}
            for (int i = 0; i < objects.Count; i++)
			{
				ClassicObject classicObject = objects[i];
				if (classicObject != null && classicObject.solids)
				{
					draw_object(classicObject);
				}
			}

            for (int j = 0; j < objects.Count; j++)
            {
                ClassicObject classicObject2 = objects[j];
                if (classicObject2 != null && classicObject2 is cage_door)
                {
                    draw_object(classicObject2);
                }
            }
            for (int j = 0; j < objects.Count; j++)
			{
				ClassicObject classicObject2 = objects[j];
				if (classicObject2 != null && classicObject2 is not cage_door)
				{
					draw_object(classicObject2);
				}
			}

			//foreach (Particle particle in particles)
			//{
			//    particle.x += particle.spd;
			//    particle.y += E.sin(particle.off);
			//    particle.off += E.min(0.05f, particle.spd / 32f);
			//    E.rectfill(particle.x, particle.y, particle.x + (float)particle.s, particle.y + (float)particle.s, particle.c);
			//    if (particle.x > 132f)
			//    {
			//        particle.x = -4f;
			//        particle.y = E.rnd(128f);
			//    }
			//}

			for (int num3 = dead_particles.Count - 1; num3 >= 0; num3--)
			{
				DeadParticle deadParticle = dead_particles[num3];
				deadParticle.x += deadParticle.spd.X;
				deadParticle.y += deadParticle.spd.Y;
				deadParticle.t--;
				if (deadParticle.t <= 0)
				{
					dead_particles.RemoveAt(num3);
				}

				E.rectfill(deadParticle.x - (float)(deadParticle.t / 5), deadParticle.y - (float)(deadParticle.t / 5), deadParticle.x + (float)(deadParticle.t / 5), deadParticle.y + (float)(deadParticle.t / 5), 14 + deadParticle.t % 2);
			}

			E.rectfill(-5f, -5f, -1f, 133f, 0f);
			E.rectfill(-5f, -5f, 133f, -1f, 0f);
			E.rectfill(-5f, 128f, 133f, 133f, 0f);
			E.rectfill(128f, -5f, 133f, 133f, 0f);
			if (is_title())
			{
				E.print("dash to begin", 42f, 96f, 5f);
			}

			if (level_index() != 30)
			{
				return;
			}

			ClassicObject classicObject3 = null;
			foreach (ClassicObject @object in objects)
			{
				if (@object is player)
				{
					classicObject3 = @object;
					break;
				}
			}

			if (classicObject3 != null)
			{
				float num4 = E.min(24f, 40f - E.abs(classicObject3.x + 4f - 64f));
				E.rectfill(0f, 0f, num4, 128f, 0f);
				E.rectfill(128f - num4, 0f, 128f, 128f, 0f);
			}
		}
		public void draw_object(ClassicObject obj)
		{
			obj.draw();
		}
		public void draw_time(int x, int y)
		{
			int num = seconds;
			int num2 = minutes % 60;
			int num3 = E.flr(minutes / 60);
			E.rectfill(x, y, x + 32, y + 6, 0f);
			E.print(((num3 < 10) ? "0" : "") + num3 + ":" + ((num2 < 10) ? "0" : "") + num2 + ":" + ((num < 10) ? "0" : "") + num, x + 1, y + 1, 7f);
		}

		public float clamp(float val, float a, float b)
		{
			return E.max(a, E.min(b, val));
		}
		public float appr(float val, float target, float amount)
		{
			if (!(val > target))
			{
				return E.min(val + amount, target);
			}

			return E.max(val - amount, target);
		}
		public bool maybe()
		{
			return E.rnd(1f) < 0.5f;
		}
		public bool solid_at(float x, float y, float w, float h)
		{
			return tile_flag_at(x, y, w, h, 0);
		}
		public bool tile_flag_at(float x, float y, float w, float h, int flag)
		{
			for (int i = (int)E.max(0f, E.flr(x / 8f)); (float)i <= E.min(15f, (x + w - 1f) / 8f); i++)
			{
				for (int j = (int)E.max(0f, E.flr(y / 8f)); (float)j <= E.min(15f, (y + h - 1f) / 8f); j++)
				{
					if (E.fget(tile_at(i, j), flag))
					{
						return true;
					}
				}
			}

			return false;
		}
		public int tile_at(int x, int y)
		{
			return E.mget(room.X * 16 + x, room.Y * 16 + y);
		}
		public bool spikes_at(float x, float y, int w, int h, float xspd, float yspd)
		{
			for (int i = (int)E.max(0f, E.flr(x / 8f)); (float)i <= E.min(15f, (x + (float)w - 1f) / 8f); i++)
			{
				for (int j = (int)E.max(0f, E.flr(y / 8f)); (float)j <= E.min(15f, (y + (float)h - 1f) / 8f); j++)
				{
					int num = tile_at(i, j);
					if (num == 17 && (E.mod(y + (float)h - 1f, 8f) >= 6f || y + (float)h == (float)(j * 8 + 8)) && yspd >= 0f)
					{
						return true;
					}

					if (num == 27 && E.mod(y, 8f) <= 2f && yspd <= 0f)
					{
						return true;
					}

					if (num == 43 && E.mod(x, 8f) <= 2f && xspd <= 0f)
					{
						return true;
					}

					if (num == 59 && ((x + (float)w - 1f) % 8f >= 6f || x + (float)w == (float)(i * 8 + 8)) && xspd >= 0f)
					{
						return true;
					}
				}
			}

			return false;
		}
		public class DeadParticle
		{
			public float x;

			public float y;

			public int t;

			public Vector2 spd;
		}
		public List<DeadParticle> dead_particles;
		public bool instakill = false;
		public void Init(StolenEmulator emulator)
		{
			E = emulator;
			room = new Point(0, 0);
			objects = new List<ClassicObject>();
			freeze = 0;
			will_restart = false;
			delay_restart = 0;
			got_fruit = new HashSet<int>();
			has_dashed = false;
			sfx_timer = 0;
			has_key = false;
			pause_player = false;
			flash_bg = false;
			music_timer = 0;
			new_bg = false;
			room_just_loaded = false;
			frames = 0;
			seconds = 0;
			minutes = 0;
			deaths = 0;
			max_djump = 1;
			start_game = false;
			start_game_flash = 0;
			//clouds = new List<Cloud>();
			//for (int i = 0; i <= 16; i++)
			//{
			//    clouds.Add(new Cloud
			//    {
			//        x = E.rnd(128f),
			//        y = E.rnd(128f),
			//        spd = 1f + E.rnd(4f),
			//        w = 32f + E.rnd(32f)
			//    });
			//}

			//particles = new List<Particle>();
			//for (int j = 0; j <= 32; j++)
			//{
			//    particles.Add(new Particle
			//    {
			//        x = E.rnd(128f),
			//        y = E.rnd(128f),
			//        s = E.flr(E.rnd(5f) / 4f),
			//        spd = 0.25f + E.rnd(5f),
			//        off = E.rnd(1f),
			//        c = 6 + E.flr(0.5f + E.rnd(1f))
			//    });
			//}

			dead_particles = new List<DeadParticle>();
			title_screen();

		}
	}
}
