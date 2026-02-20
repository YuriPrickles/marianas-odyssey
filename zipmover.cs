using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.Mod.OdysseyHelper.Zeldalike;

namespace Celeste.Mod.OdysseyHelper
{
    public class zipmover : enemy
	{
		Vector2 saved_pos;
		public int wait_timer;
		bool yellow = false;
		float accel = 1.12f;
		float max_speed = 3f;
		float speed = 0.7f;
		int vul_timer = 45;
		int max_vul = 45;
		public bool boss_attack = false;
		public Dictionary<Vector2, float> yellow_sprite_array = [];
		public override void init(Zeldalike g, StolenEmulator e)
        {
            base.init(g, e);
            ignore_solids = true;
			if (!boss_attack)
				wait_timer = 140 + (int)E.rnd(4) * 20;
			hurt_while_vulnerable = true;
			sprite_array = new Dictionary<Vector2, float>
			{
				[new Vector2(0, 0)] = 41f,
                [new Vector2(0, 8)] = 57f
			};
			yellow_sprite_array = new Dictionary<Vector2, float>
            {
                [new Vector2(0, 0)] = 42f,
                [new Vector2(0, 8)] = 58f
            };
			vul_sprite_array = new Dictionary<Vector2, float>
            {
                [new Vector2(0, 0)] = 43f,
                [new Vector2(0, 8)] = 59f
            };
			health = boss_attack || G.instakill ? 1 : 5;
        }
		public zipmover(bool b_a = false)
		{
			boss_attack = b_a;
		}
		public override void update()
		{
			hitbox = new Rectangle(3, 10, 5, 4);
			player p = find_player();
			if (p == null)
				return;
			if (wait_timer > 0)
			{
				if (boss_attack && wait_timer > 70)
                {
                    for (int i = 0; i <= 7; i++)
                    {
                        float num = (float)i / 8f;
                        G.dead_particles.Add(new DeadParticle
                        {
                            x = x + 4f,
                            y = y + 4f,
                            t = 10,
                            spd = new Vector2(E.cos(num) * 3f, E.sin(num + 0.5f) * 3f)
                        });
                    }
                    E.sfx(6);
                    G.destroy_object(this);
                }
				speed = 0.7f;
				vulnerable = false;
				vul_timer = max_vul;
				yellow = (wait_timer < 30 && wait_timer > 0);
				if (!yellow)
					saved_pos = new Vector2(p.x - 4, p.y - 4);
				wait_timer--;
				base.update();
				return;
			}
			if (vul_timer > 0)
				vul_timer--;
			else
				wait_timer = 80;
			vulnerable = true;
			speed = E.min(max_speed, speed * accel);
				
			Vector2 dir = (saved_pos - (pos + new Vector2(-4,4))).SafeNormalize(Vector2.UnitY);
			if (Vector2.Distance(pos + new Vector2(-4, 4),saved_pos) > 2)
				move(dir.X * speed, dir.Y * speed);

			base.update();
		}
        public override void on_collide(player player)
        {
            base.on_collide(player);
        }
        public override void hurt()
		{
			player p = find_player();
			p.iframes = 15;
			base.hurt();
		}
		public override void draw()
		{
			float spr_off = wait_timer < 30 && wait_timer > 0 ? (wait_timer % 2 == 0 ? -1 : 1) : 0;
			foreach (var sprite in vulnerable ? vul_sprite_array : yellow? yellow_sprite_array : sprite_array)
			{
				E.spr(sprite.Value, x + sprite.Key.X + spr_off, y + sprite.Key.Y);
			}
		}
	}
	
}
