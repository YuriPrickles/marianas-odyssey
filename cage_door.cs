using Microsoft.Xna.Framework;

namespace Celeste.Mod.OdysseyHelper
{
    public partial class Zeldalike
	{
        public class cage_door : ClassicObject
		{
			public override void init(Zeldalike g, StolenEmulator e)
			{
				hitbox = new Rectangle(0, 0, 8, 8);
				collideable = true;
				solids = true;
				base.init(g, e);

			}
			public override void update()
			{
				foreach (ClassicObject obj in G.objects)
				{
					if (obj != null && obj is enemy && obj is not spikes && obj is not melvin)
					{
						return;
					}
				}
				spr = 0f;
				G.destroy_object(this);
			}
			public override void on_destroy()
			{
				E.sfx(51);
				E.sfx(9);
				E.sfx(15);
				base.on_destroy();
			}
		}
	}
}
