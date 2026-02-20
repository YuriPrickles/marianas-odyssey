using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.OdysseyHelper
{
	public class melvin : enemy
	{
		public int switch_timer = 0;
		public string vul_dir = "UP";
		public bool crushing = false;
		Vector2 center = new Vector2();
		bool detected = false;
		int crush_delay = 0;
		public Vector2 crush_dir = Vector2.UnitY;
		float accel = 1.25f;
		float max_speed = 2f;
		float speed = 0.3f;

		public bool boss_attack = false;
		public melvin(bool b_a=false)
		{
			if (b_a)
			{
				boss_attack = true;
				crush_delay = 15;
			}
		}
		public override void init(Zeldalike g, StolenEmulator e)
		{
			hitbox = new Rectangle(0, 0, 16, 16);
			base.init(g, e);
			health = 3;
			vulnerable = false;
			hurt_while_vulnerable = true;

			sprite_array = new Dictionary<Vector2, float>
			{
				[new Vector2(0, 0)] = 98f,
				[new Vector2(8, 0)] = 99f,
				[new Vector2(0, 8)] = 114,
				[new Vector2(8, 8)] = 115,
			};
			player p = find_player();
		}
		public override void update()
		{
			center = new Vector2(x + 8, y + 8);
			player p = find_player();
			if (boss_attack)
			{
				vulnerable = false;
                if (crush_delay == 14)
                {
                    crush_dir = p.x < center.X ? new Vector2(-1, 0) : new Vector2(1, 0);
                }
                if (crush_delay > 0)
				{
					crush_delay--;
				}
				else
				{
					crushing = true;
				}
			}
			if (p != null)
			{
				if (!boss_attack)
				{
					if (switch_timer > 0)
					{
						switch_timer--;
					}
					else if (crush_delay > 0)
					{
						crush_delay--;
					}
					else if (detected)
					{
						crushing = true;
						detected = false;
					}
					else
					{
						detect_player(p);
					}
				}

				if (!crushing)
				{
					speed = 0.7f;
				}
				else
				{
					speed = E.min(max_speed, speed * accel);
					move(crush_dir.X * speed, crush_dir.Y * speed);
					if (is_solid((int)crush_dir.X, (int)crush_dir.Y))
					{
						E.sfx(0);
						crushing = false;
						if (boss_attack)
							G.destroy_object(this);
					}
				}

			}
			base.update();
		}
		public override void on_collide(player player)
		{
			if (player != null)
			{
				player.dash_effect_time = 0;
				player.dash_time = 1;
				player.hurt();
			}
		}
		public void detect_player(player player)
		{
			if (crush_delay > 0 || crushing || boss_attack) return;

			if ((player.y < center.Y &&
				x < player.x + 4 &&
				player.x < x + 16) ||
				(player.y > center.Y &&
				x < player.x + 4 &&
				player.x < x + 16) ||
				(player.x < center.X &&
				y < player.y + 4 &&
				player.y + 4 < y + 16) ||
				(player.x > center.X &&
				y < player.y + 4 &&
				player.y + 4 < y + 16))
			{
				if (player.y < center.Y &&
					x < player.x && 
					player.x < x + 16)
				{
					crush_dir = new Vector2(0, -1);
				}
				if (player.y > center.Y &&
					x < player.x &&
					player.x < x + 16)
				{
					crush_dir = new Vector2(0, 1);
				}
				if (player.x < center.X &&
					y < player.y + 4 &&
					player.y + 4 < y + 16)
				{
					crush_dir = new Vector2(-1, 0);
				}
				if (player.x > center.X &&
					y < player.y + 4 &&
					player.y + 4 < y + 16)
				{
					crush_dir = new Vector2(1, 0);
				}
				if (is_solid((int)crush_dir.X * 2,(int)crush_dir.Y * 2))
				{
					return;
				}
				E.sfx(7);
				detected = true;
				crush_delay = 15;
			}
				
		}
		public override void on_destroy()
		{
			base.on_destroy();
		}
		float playerXdir = 0;
		float playerYdir = 0;
		public override void draw()
		{
			base.draw();
			player p = find_player();
			if (p != null)
			{
				playerXdir = (p.x > center.X) ? 1 : -2;
				playerYdir = (p.y > center.Y) ? 1 : -2;
			}
			Vector2 le = new(center.X + playerXdir, center.Y + playerYdir);
			E.rectfill(le.X, le.Y, le.X + 1, le.Y + 1, 0);
		}
	}

}
