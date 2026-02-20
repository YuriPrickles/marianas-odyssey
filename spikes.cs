using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.OdysseyHelper
{
	public class spikes : enemy
	{
		public int switch_timer = 0;
		public bool permanent = false;
		bool raised = false;
		int raise_timer = 0;
		bool triggered = false;
		public override void init(Zeldalike g, StolenEmulator e)
		{
			base.init(g, e);
			vulnerable = false;
		}
		public override void update()
		{
			if (permanent)
			{
				raised = true;
			}
			else
			{
				if (raise_timer > 0)
					raise_timer--;
				else
					raised = false;

				if (switch_timer > 0)
					switch_timer--;
				else if (triggered)
                {
                    E.sfx(9);
                    raise_timer = 60;
					raised = true;
					triggered = false;
				}
			}
			base.update();
		}
		public override void on_collide(player player)
		{
			if (raised)
			{
				if (player != null)
				{
					player.hurt();
				}
			}
			else if (switch_timer <= 0 && raise_timer <= 0 && !triggered)
			{
				if (player != null)
				{
					E.sfx(54);
					switch_timer = 30;
					triggered = true;
				}
			}
		}

		public override void draw()
		{
			E.spr(raised ? 88 : 72, x, y);
		}
	}

}

