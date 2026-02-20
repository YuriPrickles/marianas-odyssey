using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using Microsoft.Xna.Framework;
using System.IO;
using Celeste.Mod.Entities;

namespace Celeste.Mod.OdysseyHelper
{
    public class AbbyDemonChaser : BadelineOldsite
    {
        public Sprite trueSprite;
        public AbbyDemonChaser(EntityData data, Vector2 offset, int index) : base(data, offset, index) { }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            Remove(Hair,Sprite);
            Session session = SceneAs<Level>().Session;
            SpriteBank sprBank = new SpriteBank(GFX.Game, Path.Combine("Graphics", "OdysseyOfSand", "NotForMetadataSprites.xml"));
            Add(trueSprite = sprBank.Create("abby_demon"));
            trueSprite.Visible = true;
            trueSprite.Play("sit");
            trueSprite.Color = Color.Transparent;
            Visible = true;
            Add(new Coroutine(StartChasingRoutine(SceneAs<Level>())));
        }
        float hovTimer = 0;
        public override void Update()
        {
            base.Update();
            Hovering = true;
            Depth = -15000;
            trueSprite.Scale.X = Sprite.Scale.X;
            if (Hovering)
            {
                trueSprite.Y = (float)(Math.Sin(hoveringTimer * 5f) * 4.0) + 2;
            }
            else
            {
                trueSprite.Y = Calc.Approach(Sprite.Y, 0f, Engine.DeltaTime * 4f) + 2;
            }
        }
    }
}
