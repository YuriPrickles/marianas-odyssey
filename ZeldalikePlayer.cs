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
using Monocle;
using MonoMod;
using SimplexNoise;
using Celeste.Mod.OdysseyHelper;
using static Celeste.Mod.OdysseyHelper.Zeldalike;

namespace Celeste.Mod.OdysseyHelper
{
	public class player : ClassicObject
	{
		public bool p_jump;

		public bool p_dash;

		public int grace;

		public int jbuffer;

		public int djump;

		public int dash_time;

		public int dash_effect_time;

		public Vector2 dash_target = new Vector2(0f, 0f);

		public Vector2 dash_accel = new Vector2(0f, 0f);

		public float spr_off;

		public bool was_on_ground;

		public player_hair hair;

		public int health = 6;

		public float iframe_max = 30f;
		public float iframes = 0f;

		public int lastAimX = 0;
		public int lastAimY = -1;
		public override void init(Zeldalike g, StolenEmulator e)
		{
			base.init(g, e);
			spr = 1f;
			djump = g.max_djump;
			hitbox = new Rectangle(1, 3, 6, 5);
		}

		public void hurt()
		{
			if (iframes > 0f) return;
			iframes = iframe_max;
            E.sfx(8);
            health--;
		}
		public override void update()
		{

			if (G.pause_player)
			{
				return;
			}
			G.p_iframing = (iframes % 5f == 0 && iframes > 0f);
			if (iframes > 0f)
				iframes--;
			lastAimX = E.dashDirectionX((!flipX) ? 1 : (-1)) != 0? E.dashDirectionX((!flipX) ? 1 : (-1)) : 0;
			lastAimY = E.dashDirectionY((!flipX) ? 1 : (-1)) != 0 ? E.dashDirectionY((!flipX) ? 1 : (-1)) : 0;

			int num = (E.btn(G.k_right) ? 1 : (E.btn(G.k_left) ? (-1) : 0));

			int yMove = (E.btn(G.k_down) ? 1 : (E.btn(G.k_up) ? (-1) : 0));
			if (health <= 0)
			{
				G.kill_player(this);
			}

			bool flag = dash_time > 0;

			bool flag3 = E.btn(G.k_dash) && !p_dash;
			p_dash = E.btn(G.k_dash);

			if (flag)
			{
				grace = 6;
				if (djump < G.max_djump)
				{
					G.psfx(54);
					djump = G.max_djump;
				}
			}
			else if (grace > 0)
			{
				grace--;
            }
            if (dash_effect_time > 0)
                dash_effect_time--;
			if (!flag)
				dash_effect_time = 0;
            if (dash_time > 0)
			{
				//G.init_object(new smoke(), x, y);
				dash_time--;
				spd.X = G.appr(spd.X, dash_target.X, dash_accel.X);
				spd.Y = G.appr(spd.Y, dash_target.Y, dash_accel.Y);
			}
			else
            {
                if (spd.X != 0f)
				{
					flipX = spd.X < 0f;
				}

				int num6 = 3;
				float num7 = (float)num6 * 0.707106769f;
				if (djump > 0 && flag3)
				{
					//G.init_object(new smoke(), x, y);
					djump--;
					dash_time = 4;
					G.has_dashed = true;
					dash_effect_time = 18;
					Vector2 norm = Vector2.Normalize(new Vector2(lastAimX,lastAimY));
					int num8 = lastAimX;
					int num9 = lastAimY;
					if (num8 != 0 && num9 != 0)
					{
						spd.X = (float)norm.X * num6;
						spd.Y = (float)norm.Y * num6;
					}
					else if (num8 != 0)
					{
						spd.X = norm.X * num6;
						spd.Y = 0;
					}
					else if (num9 != 0)
					{
						spd.X = 0;
						spd.Y = norm.Y * num6;
					}
					else
					{
						spd.X = ((!flipX) ? 1 : (-1));
						spd.Y = 0f;
					}

					G.psfx(3);
					G.freeze = 2;
					if (Settings.Instance.ScreenShake != 0)
                    {
                        G.shake = 6;
                    }
					dash_target.X = 5 * E.sign(spd.X);
					dash_target.Y = 5 * E.sign(spd.Y);
					dash_accel.X = 1.5f;
					dash_accel.Y = 1.5f;
					if (spd.X != 0f)
					{
						dash_target.X *= 0.5f;
					}
					if (spd.Y != 0f)
					{
						dash_target.Y *= 0.5f;
					}

					if (spd.Y != 0f)
					{
						dash_accel.X *= 0.707106769f;
					}

					if (spd.X != 0f)
					{
						dash_accel.Y *= 0.707106769f;
					}
				}
				else if (flag3 && djump <= 0)
				{
					G.psfx(9);
					//G.init_object(new smoke(), x, y);
				}
			}

			spr_off += 0.25f;
			if (!flag3)
			{
				int num3 = 3;
				float amount = 0.6f;
				float amount2 = 0.15f;

				if (E.abs(spd.X) > (float)num3 && E.abs(spd.Y) > (float)num3)
				{
					spd.X = G.appr(spd.X, E.sign(spd.X) * 1, amount2);
					spd.Y = G.appr(spd.Y, E.sign(spd.Y) * 1, amount2);
				}
				else
				{
					spd.X = G.appr(spd.X, num * 1, amount);
					spd.Y = G.appr(spd.Y, yMove * 1, amount);
				}
			}
			if (!flag)
			{
				//if (is_solid(num, 0))
				//{
				//	spr = 5f;
				//}
				//else
				//{
				//	spr = 3f;
				//}
			}
			float dashspr_offset = 10f;
			if (dash_effect_time <= 1)
            {
				dashspr_offset = 0f;
			}
			if (E.btn(G.k_down))
			{
				spr = 1f + dashspr_offset;
			}
			else if (E.btn(G.k_up))
			{
				spr = 3f + dashspr_offset;
			}
			else if (E.btn(G.k_right))
			{
				spr = 4f + dashspr_offset;
			}
			else if (E.btn(G.k_left))
			{
				spr = 4f + dashspr_offset;
			}
			//else if (spd.X == 0f || (!E.btn(G.k_left) && !E.btn(G.k_right)))
			//{
			//	spr = 1f;
			//}
			//else
			//{
			//	spr = 1f + spr_off % 4f;
			//}

			if (y < -4f && G.level_index() < 30)
			{
				G.next_room();
			}

			was_on_ground = flag;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override void draw()
		{
			int saved_i_value = 0;
			for (int i = 0; i < E.max(health, 6); i++)
			{
				E.spr(i + 1 <= health? (G.p_iframing? 29f: 27f) : 28f, 2 + ((i % 2 != 0 ? 0 : 4)) + (i * 4), 118, 1,1, i % 2 != 0);
				if (i % 2 != 0)
				{
					saved_i_value = i;
				}
			}
			if (x < -1f || x > 121f)
			{
				x = G.clamp(x, -1f, 121f);
				spd.X = 0f;
			}
			if (spr == 3f)
			{
				G.draw_player(this, djump);
				hair.draw_hair(this, (!flipX) ? 1 : (-1), djump);
			}
			else
			{
				hair.draw_hair(this, (!flipX) ? 1 : (-1), djump);
				G.draw_player(this, djump);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public player()
		{
		}
	}

	public class player_hair
	{
		public class node
		{
			public float x;

			public float y;

			public float size;
		}
		public node[] hair = new node[5];
		public StolenEmulator E;
		public Zeldalike G;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public player_hair(ClassicObject obj)
		{
			E = obj.E;
			G = obj.G;
			for (int i = 0; i <= 4; i++)
			{
				hair[i] = new node
				{
					x = obj.x,
					y = obj.y,
					size = E.max(1f, E.min(2f, 3 - i))
				};
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public void draw_hair(ClassicObject obj, int facing, int djump)
		{
			int num = djump switch
			{
				2 => 7 + E.flr(G.frames / 3 % 2) * 4,
				1 => 8,
				_ => 7,
			};
			Vector2 vector = new Vector2(obj.x + 4f - (float)(facing * 2), obj.y + (float)(E.btn(G.k_down) ? 4 : 3));
			node[] array = hair;
			foreach (node node in array)
			{
				node.x += (vector.X - node.x) / 1.5f;
				node.y += (vector.Y + 0.5f - node.y) / 1.5f;
				E.circfill(node.x, node.y, node.size, num);
				vector = new Vector2(node.x, node.y);
			}
		}
	}
}
