using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.OdysseyHelper
{
    public class spirit : enemy
	{
		public int switch_timer = 0;
		public float speed = 0.05f;
		public bool boss_attack = false;
		public float offset_increase = 0.04f;
		public override void init(Zeldalike g, StolenEmulator e)
		{
			base.init(g, e);
			vulnerable = true;
			sprite_array = new Dictionary<Vector2, float>
			{
				[new Vector2(0, 0)] = 15f
			};
			vul_sprite_array = new Dictionary<Vector2, float>
            {
                [new Vector2(0, 0)] = 31f
            };
			health = G.instakill ? 1 : 3;

		}
		float offset = 0;
		public float x_offset = 0;
		public override void update()
		{
			hitbox = new Rectangle(2, 2, 4, 4);
			if (!boss_attack)
			{
				if (switch_timer > 0)
					switch_timer--;
				else
				{
					vulnerable = true;
				}
			}
			if (!boss_attack)
				speed = vulnerable ? 0.05f : 0.4f;
			player p = find_player();
			if (p != null)
            {
				if (!boss_attack)
				{
					Vector2 p_pos = new Vector2(p.x, p.y);
					Vector2 dir = Vector2.Normalize(p_pos - pos);
					move(dir.X * speed, dir.Y * speed);
				}
				else
                {
					ignore_solids = true;
					vulnerable = false;
					offset += offset_increase;
                    x = 2 * E.sin(offset) + x_offset;
                    y += speed;
					if (y > 132)
					{
						G.destroy_object(this);
					}
                }
			}
			base.update();
		}
		public void go_crazy()
		{
			vulnerable = false;
			switch_timer = 120;
		}
		public override void on_destroy()
		{
			foreach (var obj in G.objects)
			{
				if (obj is spirit && obj != this && !boss_attack)
				{
					(obj as spirit).go_crazy();
				}
			}
			base.on_destroy();
		}
        public override void draw()
        {
            base.draw();
            player p = find_player();
			Vector2 center = pos + new Vector2(4,0);
			float playerXdir = 0;
            float playerYdir = 0;
			if (p != null)
			{
				playerXdir = (p.x > center.X) ? 1 : -1;
                playerYdir = (p.y > center.Y) ? 1 : -1;
            }
			Vector2 le = new(x + 2 + playerXdir, y + 3 + playerYdir);
            Vector2 re = new(x + 5 + playerXdir, y + 3 + playerYdir);
            E.rectfill(le.X, le.Y, le.X, le.Y, vulnerable? 11f : 8f);
            E.rectfill(re.X, re.Y, re.X, re.Y, vulnerable ? 11f : 8f);
        }
    }
	
}
