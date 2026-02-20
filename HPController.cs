using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.Entities;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using FMOD.Studio;
using System.Text.RegularExpressions;

namespace Celeste.Mod.OdysseyHelper
{
    public class HPController : Entity
    {
        public int health;
        public int maxHealth;
        public Coroutine mainCoroutine;
        public Coroutine regenCoroutine;
        public int regenAmount = 2;
        public PlayerHPRenderer hpRenderer;
        public Coroutine flashTextCoroutine;
        public Color renderColor;
        public class PlayerHPRenderer : Entity
        {
            public HPController hpCon;
            public PlayerHPRenderer(HPController hpc)
            {
                hpCon = hpc;
                Tag = Tags.HUD;
            }

            public override void Render()
            {
                Vector2 renderPos = new Vector2(96, Celeste.ViewHeight - 64);
                string text = $"{hpCon.health}";
                if (!Scene.Paused && hpCon.Visible)
                {
                    ActiveFont.DrawOutline(text, renderPos, Vector2.One / 2, Vector2.One * 2, hpCon.renderColor, 2, Color.Black);
                    
                }
                base.Render();
            }
        }
        public HPController(int m_hp)
        {
            maxHealth = health = m_hp;
            Add(mainCoroutine = new Coroutine(removeOnComplete: false));
            Add(regenCoroutine = new Coroutine(removeOnComplete: false));
            Add(flashTextCoroutine = new Coroutine(removeOnComplete: false));
            renderColor = Color.White;
        }
        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            scene.Add(hpRenderer = new PlayerHPRenderer(this));
        }
        public override void Update()
        {
            base.Update();
            if (regenCoroutine != null && !regenCoroutine.Active && health < maxHealth)
            {
                regenCoroutine.Replace(RegenDelay());
            }
        }
        public void Hurt(Player player, int damage)
        {
            if (mainCoroutine != null && !mainCoroutine.Active)
            {
                mainCoroutine.Replace(HurtRoutine(player,damage));
            }
        }
        public IEnumerator FlashText(Color color, Ease.Easer easing, float duration)
        {
            renderColor = color;
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, easing, duration, start: true);
            tween.OnUpdate = (Tween t) =>
            {
                renderColor = (Color.Lerp(renderColor, Color.White, t.Eased));
            };
            Add(tween);
            yield return duration;
            flashTextCoroutine.Cancel();
        }
        public IEnumerator HurtRoutine(Player player, int damage)
        {
            flashTextCoroutine.Cancel();
            flashTextCoroutine.Replace(FlashText(Color.Red, Ease.CubeOut, 1.5f));
            health = Math.Max(health - damage, 0);
            if (health <= 0)
            {
                player.Die(Vector2.UnitY);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    yield return PlayerBlink(player);
            }
            FlashText(Color.Red,Ease.CubeOut,3.5f);
            regenCoroutine.Cancel();
            mainCoroutine.Cancel();
            yield return null;
        }
        public IEnumerator PlayerBlink(Player player)
        {
            player.Visible = false;
            yield return 0.05f;
            player.Visible = true;
        }
        public IEnumerator RegenDelay()
        {
            yield return 5f;
            flashTextCoroutine.Cancel();
            flashTextCoroutine.Replace(FlashText(Color.Lime, Ease.CubeOut, 1.5f));
            while (true)
            {
                yield return RegenMain(regenAmount);
            }
        }
        public IEnumerator RegenMain(int regen)
        {
            if (health >= 100) yield break;
            health = Math.Min(health + regen,maxHealth);
            flashTextCoroutine.Cancel();
            flashTextCoroutine.Replace(FlashText(Color.LightGreen, Ease.CubeInOut, 0.3f));
            yield return 0.35f;
        }
    }
}
