using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using Microsoft.Xna.Framework;
using System.IO;

namespace Celeste.Mod.OdysseyHelper
{
    public class PatientBadelineOldsite : BadelineOldsite
    {
        public Sprite trueSprite;
        public PatientBadelineOldsite(Vector2 position, int index) : base(position, index) { }

        [MonoModLinkTo("Monocle.Entity", "System.Void Added(Monocle.Scene)")]
        private void Entity_Added(Scene scene) { }

        public override void Added(Scene scene)
        {
            Entity_Added(scene);
            Remove(Hair,Sprite);
            Session session = SceneAs<Level>().Session;
            SpriteBank sprBank = new SpriteBank(GFX.Game, Path.Combine("Graphics", "OdysseyOfSand", "NotForMetadataSprites.xml"));
            Add(trueSprite = sprBank.Create("abby_demon"));
            trueSprite.Visible = true;
            trueSprite.Play("sit");
            Visible = true;
            session.Audio.Music.Event = null;
            session.Audio.Apply(forceSixteenthNoteHack: false);
        }
        float hovTimer = 0;
        public override void Update()
        {
            base.Update();
            Depth = -15000;
            trueSprite.Scale.X = Sprite.Scale.X;
            if (Hovering)
            {
                hovTimer += Engine.DeltaTime;
                trueSprite.Y = (float)(Math.Sin(hovTimer * 2f) * 4.0) + 2;
            }
            else
            {
                trueSprite.Y = Calc.Approach(Sprite.Y, 0f, 0) + 0;
            }
        }
    }
}
