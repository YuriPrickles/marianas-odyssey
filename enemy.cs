using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Celeste.Mod.OdysseyHelper.Zeldalike;

namespace Celeste.Mod.OdysseyHelper
{
	public class enemy : ClassicObject
	{
		public Dictionary<Vector2, float> sprite_array = [];
		public Dictionary<Vector2, float> vul_sprite_array = [];
		public bool vulnerable = false;
		public bool hurt_while_vulnerable = false;
		public int health = 3;
		public Vector2 pos;
		public int iframes = 0;
		public override void init(Zeldalike g, StolenEmulator e)
		{
			base.init(g, e);
		}
		public override void update()
		{
			if (health == 0)
				G.destroy_object(this);
			if (iframes > 0)
				iframes--;
			pos = new Vector2(x, y);
			player player = collide<player>(0, 0);
			if (player != null)
				on_collide(player);
		}
		public virtual void on_collide(player player)
		{
			if (vulnerable)
			{
				if (player != null)
				{
					if (player.dash_effect_time > 0)
					{
						player.spd.X *= -1f;
						player.spd.Y *= -1f;
						player.dash_time = 1;
						player.dash_effect_time = 0;
						hurt();
					}
					else if (hurt_while_vulnerable)
					{
						player.hurt();
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
		public virtual void hurt()
		{
			if (iframes > 0) return;
			G.sfx_timer = 20;
			iframes = 5;
			health--;
            E.sfx(health == 0? 6 : 7);
        }
		public override void draw()
		{
			foreach (var sprite in vulnerable? vul_sprite_array : sprite_array)
			{
				E.spr(sprite.Value, x + sprite.Key.X, y + sprite.Key.Y);
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
	
}
