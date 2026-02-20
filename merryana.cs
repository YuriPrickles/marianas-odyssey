using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.Mod.OdysseyHelper.Zeldalike;

namespace Celeste.Mod.OdysseyHelper
{
	public class merryana : enemy
	{
		float accel = 1.8f;
		float max_speed = 3.6f;
		float speed = 1f;
		int dash_count = 5;

		int attack_timer = 0;
		enum FlipType
		{
			FlipNone,
			FlipX,
			FlipY,
			FlipBoth,
		}
		Dictionary<Vector2, float> glitch_sprite_array;
		Dictionary<Vector2, FlipType> glitch_flip;
		Dictionary<Vector2, float> glitch_x_offset_strength;
		bool glitch_phase = false;
		List<Vector2> melvin_spawns = [
			new Vector2(1,13) * 8,
			new Vector2(1,11) * 8,
			new Vector2(1,09) * 8,
		];
        int attack_phase = 2;
		public override void init(Zeldalike g, StolenEmulator e)
		{
			base.init(g, e);
            E.forever_trail = false;
            Audio.SetMusicParam("b_side", 0);
            vulnerable = false;
			health = G.instakill ? 1 : 25;
			sprite_array = new Dictionary<Vector2, float>
			{
				[new Vector2(0, 0)] = 64,
				[new Vector2(8, 0)] = 65,
				[new Vector2(16, 0)] = 66,
				[new Vector2(24, 0)] = 67,
				[new Vector2(0, 8)] = 80,
				[new Vector2(8, 8)] = 81,
				[new Vector2(16, 8)] = 82,
				[new Vector2(24, 8)] = 83,
			};
			glitch_sprite_array = new Dictionary<Vector2, float>
			{
				[new Vector2(-8, 0)] = 64,
				[new Vector2(-8, 8)] = 64,
				[new Vector2(-8, -8)] = 64,

				[new Vector2(32, 0)] = 64,
				[new Vector2(32, 8)] = 64,
				[new Vector2(32, -8)] = 64,

				[new Vector2(0, 8)] = 64,
				[new Vector2(0, -8)] = 64,
				[new Vector2(24, 8)] = 64,
				[new Vector2(24, -8)] = 64,
				[new Vector2(0, 0)] = 65,
				[new Vector2(24, 0)] = 65,

				[new Vector2(8, 0)] = 81,
				[new Vector2(16, 0)] = 81,
				[new Vector2(8, 8)] = 81,
				[new Vector2(16, 8)] = 81,


				[new Vector2(8, -8)] = 80,
				[new Vector2(16, -8)] = 80,

			};
			glitch_flip = new Dictionary<Vector2, FlipType>
			{
				[new Vector2(0, 0)] = FlipType.FlipY,
				[new Vector2(8, 0)] = FlipType.FlipY,
				[new Vector2(16, 0)] = FlipType.FlipBoth,
				[new Vector2(24, 0)] = FlipType.FlipBoth,

				[new Vector2(0, 8)] = FlipType.FlipBoth,
				[new Vector2(8, 8)] = FlipType.FlipNone,
				[new Vector2(16, 8)] = FlipType.FlipX,
				[new Vector2(24, 8)] = FlipType.FlipY,

				[new Vector2(-8, 0)] = FlipType.FlipNone,
				[new Vector2(-8, 8)] = FlipType.FlipNone,
				[new Vector2(-8, -8)] = FlipType.FlipY,

				[new Vector2(0, -8)] = FlipType.FlipX,
				[new Vector2(8, -8)] = FlipType.FlipY,
				[new Vector2(16, -8)] = FlipType.FlipBoth,
				[new Vector2(24, -8)] = FlipType.FlipNone,

				[new Vector2(32, 0)] = FlipType.FlipX,
				[new Vector2(32, 8)] = FlipType.FlipX,
				[new Vector2(32, -8)] = FlipType.FlipBoth,
			};
			glitch_x_offset_strength = new Dictionary<Vector2, float>
			{
				[new Vector2(-8, 0)] = -0.2f,
				[new Vector2(-8, 8)] = -0.2f,
				[new Vector2(-8, -8)] = -0.2f,

				[new Vector2(32, 0)] = 0.2f,
				[new Vector2(32, 8)] = 0.2f,
				[new Vector2(32, -8)] = 0.2f,

				[new Vector2(0, 8)] = -0.08f,
				[new Vector2(0, 0)] = -0.08f,
				[new Vector2(0, -8)] = -0.08f,
				[new Vector2(24, 8)] = 0.08f,
				[new Vector2(24, 0)] = 0.08f,
				[new Vector2(24, -8)] = 0.08f,

				[new Vector2(8, -8)] = 0.01f,
				[new Vector2(16, -8)] = 0.01f,

				[new Vector2(8, 0)] = 0,
				[new Vector2(16, 0)] = 0,
				[new Vector2(8, 8)] = 0,
				[new Vector2(16, 8)] = 0,
			};
			ignore_solids = true;
		}
		int vul_timer = 0;
		float offset = 0;
		float draw_offset = 0;
		Vector2 center;
		int cycles = 0;
		Vector2 saved_pos;
		Vector2 dir;
		public override void update()
		{
			center = new Vector2(x + 8, y + 8);
			hitbox = new Rectangle(6, 0, 20, 16);
			player p = find_player();

			if (p != null)
			{
				if (vul_timer > 0)
				{
					vulnerable = true;
					vul_timer--;
				}
				else
				{
					vulnerable = false;
				}
				attack_timer++;
				if (!glitch_phase)
                {
                    if (attack_phase == -2)
                    {
                        if (attack_timer >= -150 && attack_timer % 10 == 0)
                        {
                            G.shake = 3;
                            Vector2 particlePos = new Vector2(E.rnd(32) - 16) + center;
                            for (int i = 0; i <= 8; i++)
                            {
                                float num = (float)i / 8f;
                                G.dead_particles.Add(new DeadParticle
                                {
                                    x = particlePos.X,
                                    y = particlePos.Y,
                                    t = 10,
                                    spd = new Vector2(E.cos(num) * 3f, E.sin(num + 0.5f) * 3f)
                                });
                            }
                            G.sfx_timer = 0;
                            E.sfx(0);

                        }
                        if (attack_timer >= 0)
                        {
                            G.shake = 30;
                            attack_timer = 0;
                            attack_phase = -999;
							G.destroy_object(this);
                        }
                    }
                    if (attack_phase == 0)
					{
						if (attack_timer == 20)
						{
							melvin_spawns.Shuffle();
						}
						if (attack_timer >= 60 && attack_timer % 20 == 0)
						{
							int index = (attack_timer - 60) / 20;
							melvin mel = new melvin(true);
							mel.boss_attack = true;
							G.init_object(mel, melvin_spawns[index].X, melvin_spawns[index].Y);
							if (attack_timer >= 100)
							{
								attack_phase = 1;
								attack_timer = -60;
								vul_timer = 80;
							}
						}
					}
					if (attack_phase == 1)
					{
						if (attack_timer == 30)
							vul_timer = 120;
						if (attack_timer >= 30 && attack_timer % 8 == 0)
						{
							zipmover zip = new zipmover(true);
							zip.boss_attack = true;
							zip.wait_timer = 50;
							G.init_object(zip, center.X, center.Y - 32);
							if (attack_timer >= 90)
							{
								attack_phase = G.maybe() ? 2 : 0;
								attack_timer = -40;
							}
						}
					}
					if (attack_phase == 2)
					{
						if (attack_timer == 40)
							vul_timer = 120;
						if (attack_timer >= 30 && attack_timer % 30 == 0)
						{
							int offset = (int)E.rnd(64) - 32;
							for (int i = -6; i < 6; i++)
							{
								spirit spirit = new spirit();
								spirit.boss_attack = true;
                                spirit.speed = 1.2f;
                                spirit.x_offset = (i * 24) + center.Y + offset;
								G.init_object(spirit, 64 + (i * 24), center.Y - 160);
							}
							if (attack_timer >= 100)
							{
								attack_phase = G.maybe() ? 0 : 1;
								attack_timer = 15;
								cycles += 1;
								if (cycles >= 3)
								{
									attack_phase = 3;
									attack_timer = -60;
								}
							}
						}
					}
					if (attack_phase == 3)
					{
						if (dash_count <= 0)
						{
							cycles = 0;
							E.pal();
							attack_timer = 0;
							attack_phase = 0;
							dash_count = 3;
						}
						else
						{
							if (attack_timer < 60)
                            {
                                G.shake = 30;
                                y -= 2;
							}
							if (attack_timer == 120)
							{

								x = E.rnd(128 - 32);
								y = 32;
								saved_pos = new Vector2(p.x + 4, p.y + 4);
								for (int i = 0; i <= 7; i++)
								{
									float num = (float)i / 16f;
									G.dead_particles.Add(new DeadParticle
									{
										x = x + 16,
										y = y + 8f,
										t = 10,
										spd = new Vector2(E.cos(num) * 3f, E.sin(num + 0.5f) * 3f)
									});
								}
							}
							if (attack_timer >= 130)
							{
								if (y < 40)
									dir = (saved_pos - (center)).SafeNormalize(Vector2.UnitX);
								speed = E.min(max_speed, speed * accel);
								move(dir.X * speed, dir.Y * speed);
							}
							if (y > 160)
							{
								dash_count--;
								attack_timer = 119;
								x = E.rnd(128 - 32);
								y = -100;
							}
						}
					}
					else if (attack_phase != -2)
					{
						offset += 0.04f;
						x = 32 * E.sin(offset * 0.3f) + 48;
						y = 2f * E.sin(offset * 0.6f) + 56;
					}
				}
				else
                {
                    hitbox = new Rectangle(8, 0, 16, 16);
                    accel = 2.1f;
                    max_speed = 5.4f;
                    if (attack_phase == -3)
                    {
						E.forever_trail = true;
                        G.shake = 30;
                        if (attack_timer >= -150 && attack_timer % 10 == 0)
                        {
                            G.shake = 3;
                            Vector2 particlePos = new Vector2(E.rnd(32) - 16) + center;
                            for (int i = 0; i <= 8; i++)
                            {
                                float num = (float)i / 8f;
                                G.dead_particles.Add(new DeadParticle
                                {
                                    x = particlePos.X,
                                    y = particlePos.Y,
                                    t = 10,
                                    spd = new Vector2(E.cos(num) * 3f, E.sin(num + 0.5f) * 3f)
                                });
                            }
                            G.sfx_timer = 0;
                            E.sfx(0);
                        }
                        if (attack_timer >= 0)
                        {
                            E.forever_trail = false;
                            G.shake = 60;
                            attack_timer = 0;
                            attack_phase = 0;
							G.destroy_object(this);
                        }
                    }
                    if (attack_phase == -2)
					{
						if (attack_timer >= -240 && attack_timer % 10 == 0)
						{
							G.shake = 3;
							Vector2 particlePos = new Vector2(E.rnd(32) - 16) + center;
                            for (int i = 0; i <= 8; i++)
                            {
                                float num = (float)i / 8f;
                                G.dead_particles.Add(new DeadParticle
                                {
                                    x = particlePos.X,
                                    y = particlePos.Y,
                                    t = 10,
                                    spd = new Vector2(E.cos(num) * 3f, E.sin(num + 0.5f) * 3f)
                                });
                            }
                            G.sfx_timer = 0;
                            E.sfx(0);
                        }
						if (attack_timer >= -60)
                        {
                            Audio.SetMusicParam("b_side", 1);
                        }
						if (attack_timer >= 0)
                        {
                            G.shake = 30;
                            attack_timer = 0;
							attack_phase = 0;
						}
                    }
                    if (attack_phase == 0)
                    {
                        if (attack_timer == 20)
                        {
                            melvin_spawns = [
								new Vector2(1,13) * 8,
								new Vector2(1,11) * 8,
								new Vector2(1,09) * 8,
                                new Vector2(12,13) * 8,
                                new Vector2(12,11) * 8,
                                new Vector2(12,09) * 8,
                            ];
                            melvin_spawns.Shuffle();
                        }
                        if (attack_timer >= 60 && attack_timer % 15 == 0)
                        {
                            int index = (attack_timer - 60) / 15;
                            melvin mel = new melvin(true);
                            mel.boss_attack = true;
                            G.init_object(mel, melvin_spawns[index].X, melvin_spawns[index].Y);
                            if (attack_timer >= 135)
                            {
                                attack_phase = 1;
                                attack_timer = -60;
                                vul_timer = 80;
                            }
                        }
                    }
                    if (attack_phase == 1)
                    {
                        if (attack_timer == 30)
                            vul_timer = 90;
                        if (attack_timer >= 30 && attack_timer % 6 == 0)
                        {
                            zipmover zip = new zipmover(true);
                            zip.boss_attack = true;
                            zip.wait_timer = 30 + E.flr(E.rnd(25));
                            G.init_object(zip, 0, E.flr(E.rnd(128)) - 8);
                            if (attack_timer >= 120)
                            {
                                attack_phase = /*G.maybe() ? 2 : */2;
                                attack_timer = -40;
                            }
                        }
                    }
                    if (attack_phase == 2)
                    {
                        if (attack_timer == 40)
                            vul_timer = 120;
                        if (attack_timer >= 30 && attack_timer % 12 == 0)
                        {
                            for (int i = -1; i < 1; i++)
                            {
                                spirit spirit = new spirit();
                                spirit.boss_attack = true;
								spirit.speed = 2.1f;
								spirit.offset_increase = 0.02f;
                                spirit.x_offset = (i * 24) + ((int)E.rnd(128));
                                G.init_object(spirit, 64 + (i * 24), center.Y - 224 + E.rnd(64) - 32);
                            }
                            if (attack_timer >= 120)
                            {
                                attack_phase = G.maybe() ? 0 : 1;
                                attack_timer = 15;
                                cycles += 1;
                                if (cycles >= 3)
                                {
                                    attack_phase = 3;
                                    attack_timer = -60;
                                }
                            }
                        }
                    }
                    if (attack_phase == 3)
                    {
						E.forever_trail = true;
                        if (dash_count <= 0)
                        {
                            E.forever_trail = false;
                            cycles = 0;
                            E.pal();
                            vul_timer = 40;
                            attack_timer = 0;
                            attack_phase = 0;
                            dash_count = 6;
                        }
                        else
                        {
                            if (attack_timer < 60)
                            {
                                G.shake = 30;
                                y -= 2;
                            }
                            if (attack_timer == 120)
                            {

                                x = E.rnd(128 - 32);
                                y = 32;
                                saved_pos = new Vector2(p.x + 4, p.y + 4);
                                for (int i = 0; i <= 7; i++)
                                {
                                    float num = (float)i / 16f;
                                    G.dead_particles.Add(new DeadParticle
                                    {
                                        x = x + 16,
                                        y = y + 8f,
                                        t = 10,
                                        spd = new Vector2(E.cos(num) * 3f, E.sin(num + 0.5f) * 3f)
                                    });
                                }
                            }
                            if (attack_timer >= 130)
                            {
                                if (y < 40)
                                    dir = (saved_pos - (center)).SafeNormalize(Vector2.UnitX);
                                speed = E.min(max_speed, speed * accel);
                                move(dir.X * speed, dir.Y * speed);
                            }
                            if (y > 160)
                            {
                                dash_count--;
                                attack_timer = 119;
                                x = E.rnd(128 - 32);
                                y = -100;
                            }
                        }
                    }
					else
                    {
						if (attack_phase != -2)
							offset += 0.055f;
                        if (attack_phase == -3)
                            offset += 0.15f;
                        x = 32 * E.sin(offset * 0.3f) + 48;
                        y = 2f * E.sin(offset * 0.6f) + 56;
                    }
                }
			}
			base.update();
		}
		public override void on_destroy()
		{
			find_player().health = 6;
			Audio.SetMusic("event:/classic/sfx61");
			base.on_destroy();
		}
		public override void on_collide(player player)
		{
			if (vulnerable)
			{
				if (player != null)
				{
					if (player.dash_effect_time > 0)
					{
						player.spd.X *= -2f;
						player.spd.Y *= -2f;
						player.dash_time = 1;
						player.dash_effect_time = 0;
						player.iframes = 15;
						hurt();
					}
				}
			}
			else
			{
				if (player != null)
				{
					player.hurt();
				}
			}
		}

		public override void draw()
		{
			draw_offset += 0.05f;
			bool draw_vul_spr = vulnerable && !(vul_timer < 20 && vul_timer % 5 == 0);
			bool draw_glitch_form = glitch_phase && attack_phase != -2 && E.b_side;

            foreach (var sprite in draw_glitch_form ? glitch_sprite_array : sprite_array)
			{
				glitch_flip.TryGetValue(sprite.Key, out FlipType flip);

				glitch_x_offset_strength.TryGetValue(sprite.Key, out float o);
				Vector2 finalDrawPos = new Vector2(x + sprite.Key.X , y + sprite.Key.Y);
				if (o != 0f && draw_glitch_form)
				{
					finalDrawPos.X = ((o * 60) * E.sin(draw_offset)) + x + sprite.Key.X;
					finalDrawPos.Y -= 4;
				}
				float sprite_id = (draw_vul_spr ? sprite.Value + 4f : sprite.Value) + (glitch_phase && E.rnd(1) <= 0.04f ? (E.flr(E.rnd(4)) - 2) * 16 : 0);
				if (attack_phase == -2 && attack_timer > -55 && E.b_side)
				{
					sprite_id = E.flr(E.rnd(64)) + 32f;
				}
				E.spr(sprite_id, finalDrawPos.X, finalDrawPos.Y ,1,1, draw_glitch_form && (flip == FlipType.FlipX || flip == FlipType.FlipBoth), draw_glitch_form && (flip == FlipType.FlipY || flip == FlipType.FlipBoth));
			}

			if (attack_timer >= 119 && attack_phase == 3)
			{
				E.pal();
				E.pal(5, 0);
				E.pal(10, 8);
				E.pal(1, 8);
				E.pal(9, 0);
				E.pal(12, 8);
				E.pal(4, 2);
				E.pal(13, 2);
			}
			if (dash_count <= 0)
			{
				E.pal();
			}

			if (attack_phase == 0)
			{
				if (!glitch_phase)
                {
                    if ((attack_timer >= 30 && attack_timer < 50) ||
                        (attack_timer >= 50 && attack_timer < 70) ||
                        (attack_timer >= 70 && attack_timer < 90))
                    {
                        int index = (attack_timer - 30) / 20;
                        E.rectfill(melvin_spawns[index].X, melvin_spawns[index].Y, melvin_spawns[index].X + 16, melvin_spawns[index].Y + 16, 8f);
                    }
                }
				else
				{
                    if ((attack_timer >= 30 && attack_timer < 45) ||
                        (attack_timer >= 45 && attack_timer < 60) ||
                        (attack_timer >= 60 && attack_timer < 75) ||
                        (attack_timer >= 75 && attack_timer < 90) ||
                        (attack_timer >= 90 && attack_timer < 105) ||
                        (attack_timer >= 105 && attack_timer < 120))
                    {
                        int index = (attack_timer - 30) / 15;
                        E.rectfill(melvin_spawns[index].X, melvin_spawns[index].Y, melvin_spawns[index].X + 16, melvin_spawns[index].Y + 16, 8f);
                    }
                }
			}
		}
		public override void hurt()
		{
			if (iframes > 0) return;
			G.sfx_timer = 20;
			iframes = 5;
			health--;
			if (health == 0)
            {
                find_player().health = 6;
                attack_phase = glitch_phase ? -3 :- 2;
                attack_timer = glitch_phase ? -240 : -150;
                health = 999;
                vulnerable = false;
                if (!glitch_phase && E.b_side)
                {
                    find_player().health = 10;
                    health = G.instakill ? 1 : 30;
                    glitch_phase = true;
                    dash_count = 6;
                }
			}
			E.sfx(health == 0 ? 6 : 7);
		}
	}

}

