using Celeste.Mod.Core;
using Celeste.Mod.Entities;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Celeste.MoonGlitchBackgroundTrigger;
using static Monocle.Sprite;

namespace Celeste.Mod.OdysseyHelper
{
	public sealed class FakeCrashScreen : Overlay
	{

		private static readonly FieldInfo f_Engine_scene = typeof(Engine).GetField("scene", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly FieldInfo f_Engine_nextScene = typeof(Engine).GetField("nextScene", BindingFlags.NonPublic | BindingFlags.Instance);

		private sealed class BeforeRenderInterceptor : Renderer
		{
			public Action OnBeforeRender;
			public BeforeRenderInterceptor(Action onBeforeRender) => OnBeforeRender = onBeforeRender;
			public override void BeforeRender(Scene scene) => OnBeforeRender();
		}

		public enum DisplayState
		{
			Initial,
			Overlay,
			CleanScene,
			BlueScreen
		}

		private enum UserChoice
		{
			FlushSaveData,
			RetryLevel,
			SaveAndQuit,
			ReturnToMainMenu
		}

		public readonly ExceptionDispatchInfo Error;
		private readonly string errorType, errorMessage, errorStackTrace;
		public readonly string LogFile, LogFileError;
		public readonly Session Session;

		public bool EncounteredAdditionalErrors { get; private set; }
		private readonly BeforeRenderInterceptor beforeRenderInterceptor;
		private TextMenu optMenu;
		private readonly HashSet<UserChoice> failedChoices = new HashSet<UserChoice>();
		private bool hasFlushedSaveData;

		private DisplayState state = DisplayState.Initial;
		public DisplayState State
		{
			get => state;
			private set
			{
				state = value;
				if (optMenu != null)
					ConfigureOptionsMenu();
			}
		}

		public event Action OnClose;

		private bool disablePlayerSprite;
		private PlayerSprite playerSprite;
		private PlayerHair playerHair;
		private VirtualRenderTarget playerRenderTarget;
		private bool UsePlayerSprite => !disablePlayerSprite && State != DisplayState.BlueScreen;

		private bool playerShouldTeabag;
		private bool isCrouched;
		private float crouchTimer;
		public Player player;
		public FakeCrashScreen(Player plr)
		{
			player = plr;
			Depth += 100; // Render below other overlays
			State = DisplayState.Overlay;
            beforeRenderInterceptor = new BeforeRenderInterceptor(BeforeRender);
            Add(new Coroutine(Routine()));
        }

		public void Dispose()
		{
			playerRenderTarget?.Dispose();
			playerRenderTarget = null;
		}
		public List<string> buttonTexts = new List<string>
		{
			"...",
			"I deserve this",
			"I should be dead",
			"I should've",
			"Joined her",
			"Peaceful",
			"Serene",
			"Death",
			"...",
			"But I want",
			"To wake up",
			"Even though",
			"Reality",
			"Is worse than",
			"My worst nightmare",
			"I'm afraid",
			"Of moving on",
			"And forgiving myself",
			"I fear it",
			"More than death",
		};
		public int buttonProgress = 0;
		public bool done = false;
		//float mx = 16f;
  //      float my = 32f;
		public bool reallyDone = false;
        private IEnumerator Routine()
		{
			Level level = SceneAs<Level>();

            if (State != DisplayState.BlueScreen)
				yield return FadeIn();
			else
				Fade = 1f;

			// Create the options menu
			optMenu = new TextMenu() { AutoScroll = false };

			for (int i = 0; i < 6; i++)
            {
                TextMenu.Button button = new TextMenu.Button("Hello...?");
                button.Pressed(() => {
                    if (buttonProgress >= buttonTexts.Count)
                    {
                        done = true;
                    }
                    else
                    {
                        button.Label = buttonTexts[buttonProgress];
						foreach (TextMenu.Item butt in optMenu.Items)
						{
							if (butt is TextMenu.Button)
							{
								(butt as TextMenu.Button).Label = buttonTexts[buttonProgress];
                            }
						}
                        buttonProgress = Math.Min(buttonProgress + 1, buttonTexts.Count);
                    }
                });
                optMenu.Add(button);
			}
			ConfigureOptionsMenu();


			while (!done)
				yield return null;
			yield return FadeTuahOut();
   //         Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, 3, start: true);
   //         tween.OnUpdate = (Tween t) =>
   //         {
   //             drawPos = (Vector2.Lerp(drawPos, player.Position, t.Eased));
   //         };
			//Add(tween);
   //         Tween tween2 = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, 3, start: true);
   //         tween2.OnUpdate = (Tween t) =>
   //         {
			//	mx = MathHelper.Lerp(mx, 0, t.Eased);
   //             my = MathHelper.Lerp(my, 0, t.Eased);
   //             matrix = Matrix.CreateTranslation(mx, my, 0);
   //         };
   //         Add(tween2);
            yield return 2f;
			
            foreach (Backdrop item in level.Foreground.GetEach<Backdrop>("room_coverer"))
            {
				item.FadeAlphaMultiplier = 0;
				item.OnlyIn.Clear();
				item.ExcludeFrom.Add("0*");
            }
			playerSprite.RenderPosition = player.Position;
			reallyDone = true;
            RemoveSelf();
		}

		private void ConfigureOptionsMenu()
		{
			optMenu.ItemSpacing = 4;
			optMenu.RecalculateSize();

			if (UsePlayerSprite)
			{
				optMenu.Position = new Vector2(Celeste.TargetWidth * 0.15f, Celeste.TargetHeight * 0.55f);
				optMenu.Justify = new Vector2(0.5f, 0f);

				// Reduce item spacing if there are too many items
				if (optMenu.Position.Y + optMenu.Height > Celeste.TargetHeight * 0.85f)
				{
					optMenu.ItemSpacing = 0;
					optMenu.RecalculateSize();
				}
			}
			else
			{
				optMenu.Position = new Vector2(Celeste.TargetWidth * 0.15f, Celeste.TargetHeight * 0.6f);
				optMenu.Justify = new Vector2(0.5f, 0.5f);
			}
		}

		public override void Added(Scene scene)
		{
			Overlay oldOverlay = (scene as IOverlayHandler)?.Overlay;

			base.Added(scene);
			scene.Add(beforeRenderInterceptor);

			// Preserve the old overlay
			if (oldOverlay != null)
				((IOverlayHandler)scene).Overlay = oldOverlay;
		}

		public override void Removed(Scene scene)
		{
			scene.Remove(beforeRenderInterceptor);
			base.Removed(scene);
		}

		public override void SceneEnd(Scene scene)
		{
			// Transfer over to the new scene
			Scene newScene = f_Engine_nextScene.GetValue(Celeste.Instance) as Scene;
			if (newScene != null && scene != null)
			{
				scene.Remove(this);
				newScene.Add(this);
			}

			base.SceneEnd(scene);
		}
		Rectangle drawRect;
        Vector2 drawPos = new Vector2(Celeste.TargetWidth * 0.15f, Celeste.TargetHeight * 0.5f);
        float size = Celeste.TargetHeight * 0.65f;
        Matrix matrix = Matrix.CreateTranslation(16, 32, 0);
        public override void Update()
		{

			// Update the player state
			if (UsePlayerSprite && playerSprite != null && playerHair != null)
			{
				// Boring fall animation ._.
				if (playerSprite.LastAnimationID != "asleep")
					playerSprite.Play("asleep");

				playerSprite.Update();
				playerHair.Update();
				if (playerSprite != null)
				{
					playerSprite.Scale.X = Calc.Approach(playerSprite.Scale.X, 1, 1.75f * Celeste.DeltaTime);
					playerSprite.Scale.Y = Calc.Approach(playerSprite.Scale.Y, 1, 1.75f * Celeste.DeltaTime);
				}
			}
            drawRect = new Rectangle((int)(drawPos.X - size / 2), (int)(drawPos.Y - size), (int)size, (int)size);
            // Update the options menu
            if (Fade == 1)
				optMenu?.Update();

			base.Update();
		}

        public PlayerSprite CloneInto(PlayerSprite orig, PlayerSprite clone)
        {
            clone.Texture = orig.Texture;
            clone.Position = orig.Position;
            clone.Justify = orig.Justify;
            clone.Origin = orig.Origin;
            clone.animations = new Dictionary<string, Animation>(orig.animations, StringComparer.OrdinalIgnoreCase);
            clone.currentAnimation = orig.currentAnimation;
            clone.animationTimer = orig.animationTimer;
            clone.width = orig.width;
            clone.height = orig.height;
            clone.Animating = orig.Animating;
            clone.CurrentAnimationID = orig.CurrentAnimationID;
            clone.LastAnimationID = orig.LastAnimationID;
            clone.CurrentAnimationFrame = orig.CurrentAnimationFrame;
            return clone;
        }
        private void BeforeRender()
		{
			if (UsePlayerSprite)
			{
				try
                {
                    playerSprite = CloneInto(this.player.Sprite,new PlayerSprite(PlayerSpriteMode.MadelineNoBackpack));
                    playerSprite.RenderPosition = Vector2.Zero;
                    playerHair = this.player.Hair;
                    playerRenderTarget ??= VirtualContent.CreateRenderTarget("crit-error-handler-player", 32, 32);
					// Draw the player sprite to the render target
					Celeste.Instance.GraphicsDevice.SetRenderTarget(playerRenderTarget);
					Celeste.Instance.GraphicsDevice.Clear(Color.Transparent);

					SpriteSortMode spriteSortMode = SpriteSortMode.Deferred;
					BlendState blendState = BlendState.AlphaBlend;
					SamplerState samplerState = SamplerState.PointClamp;
					DepthStencilState depthStencilState = DepthStencilState.Default;
					RasterizerState rasterizerState = RasterizerState.CullNone;
					Effect effect = null;
					OnBeforePlayerRender?.Invoke(ref spriteSortMode, ref blendState, ref samplerState, ref depthStencilState, ref rasterizerState, ref effect, ref matrix);
					Draw.SpriteBatch.Begin(spriteSortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, matrix);

					try
					{
						playerHair.AfterUpdate();
						playerHair.Render();
						playerSprite.Render();
					}
					finally
					{
						Draw.SpriteBatch.End();
						OnAfterPlayerRender?.Invoke();
					}
				}
				catch (Exception ex)
				{
					disablePlayerSprite = true;
				}
			}
		}

		/// <summary>
		/// This event is invoked before the player sprite is being rendered to
		/// its render target. Handlers may modify the sprite batch state which
		/// is used for drawing the sprite. When invoked, there's no active
		/// sprite batch, but the render target has already been bound and
		/// cleared.
		/// </summary>
		public static event BeforePlayerRenderHandler OnBeforePlayerRender;
		public delegate void BeforePlayerRenderHandler(ref SpriteSortMode sortMode, ref BlendState blendState, ref SamplerState samplerState, ref DepthStencilState depthStencilState, ref RasterizerState rasterizerState, ref Effect effect, ref Matrix matrix);

		/// <summary>
		/// This event is invoked after the player sprite has been rendered to
		/// its render target. When invoked, the sprite batch has already been
		/// ended.
		/// </summary>
		public static event Action OnAfterPlayerRender;

		public float FadeTuah = 1f;
        public IEnumerator FadeTuahIn()
        {
            while (FadeTuah < 1f)
            {
                yield return null;
                FadeTuah += Engine.DeltaTime * 4f;
            }

            FadeTuah = 1f;
        }

        public IEnumerator FadeTuahOut()
        {
            while (FadeTuah > 0f)
            {
                yield return null;
                FadeTuah -= Engine.DeltaTime * 1.25f;
            }
        }
        public override void Render()
		{
			// Draw the background
			switch (State)
			{
				case DisplayState.Overlay:
					RenderFade();
					break;

				case DisplayState.BlueScreen:
					Draw.Rect(-10, -10, Celeste.TargetWidth + 20, Celeste.TargetHeight + 20, new Color(0x20, 0x40, 0x60));
					break;
			}

			// Draw the options menu
			if (optMenu != null)
			{
				optMenu.Alpha = Fade * FadeTuah;
				optMenu.Render();

				if (failedChoices.Count > 0)
					ActiveFont.Draw("Failed to execute user action", optMenu.Position - Vector2.UnitY * (optMenu.Height * optMenu.Justify.Y + 5), new Vector2(0.5f, 1), new Vector2(0.7f), Color.IndianRed * Fade * FadeTuah);
			}

			// Draw the player render target to the screen
			if (UsePlayerSprite && playerRenderTarget != null)
			{
				HudRenderer.EndRender();
				try
				{
					HudRenderer.BeginRender(sampler: SamplerState.PointClamp);
					try
					{
						Draw.SpriteBatch.Draw((RenderTarget2D)playerRenderTarget, drawRect, Color.White * Fade);
					}
					finally
					{
						HudRenderer.EndRender();
					}
				}
				catch (Exception ex)
				{
					disablePlayerSprite = true;
				}
				finally
				{
					HudRenderer.BeginRender();
				}
			}

			// Draw the error UI
			Vector2 textPos = new Vector2(Celeste.TargetWidth * 0.3f, Celeste.TargetHeight * 0.35f);
			ActiveFont.Draw("Oooops..?", textPos, new Vector2(0, 1), new Vector2(3), Color.White * Fade * FadeTuah);
			textPos.X += 50;

			void DrawLineWrap(string text, float scale, Color color, Vector2 posOff = default)
			{
				bool firstLine = true;
				while (firstLine || !string.IsNullOrWhiteSpace(text))
				{
					// Handle line breaking
					int lineLen;
					float availSpace = Celeste.TargetWidth * 0.95f - (textPos.X + posOff.X);
					if (ActiveFont.Measure(text).X * scale > availSpace)
					{
						// Do binary search to determine the cutoff point
						int start = 0, end = text.Length;
						while (start < end - 1)
						{
							int middle = start + (end - start) / 2;
							float textSize = ActiveFont.Measure(text.Substring(0, middle)).X * scale;
							if (textSize > availSpace)
								end = middle;
							else
								start = middle;
						}
						lineLen = start;
					}
					else
						lineLen = text.Length;

					// Draw one line, and advance to the text
					ActiveFont.Draw(text.Substring(0, lineLen), textPos + posOff, Vector2.Zero, new Vector2(scale), color * Fade * FadeTuah);
					textPos.Y += ActiveFont.LineHeight * 1.1f * scale;

					text = text.Substring(lineLen);
					if (firstLine)
						posOff.X += ActiveFont.LineHeight * 0.8f * scale;
					firstLine = false;
				}
			}

			DrawLineWrap("You have encountered a critical error", 0.7f, Color.LightGray);
			DrawLineWrap("'Make it stop!', you scream in terror", 0.7f, Color.LightGray);
			DrawLineWrap("Eyes covered from an inescapable sight", 0.5f, Color.Gray, Vector2.UnitX * 50f);
			DrawLineWrap("A corpse shining in the moonlight", 0.7f, Color.LightGray);

			textPos.Y += 20;
			DrawLineWrap("Error Details: NightmareCancelException: Nightmare ended early.", 0.7f, Color.LightGray);
			
			DrawLineWrap("What more could be done, what more could be said", 0.5f, Color.Gray, Vector2.UnitX * 50f);
			DrawLineWrap("For one to impatiently bring back the dead", 0.5f, Color.Gray, Vector2.UnitX * 50f);
			DrawLineWrap("You wish all this effort, the work you have done", 0.5f, Color.Gray, Vector2.UnitX * 50f);
			DrawLineWrap("Was done long ago, before she was gone", 0.5f, Color.Gray, Vector2.UnitX * 50f);
			DrawLineWrap("All you have now is yourself and a choice", 0.5f, Color.Gray, Vector2.UnitX * 50f);
			DrawLineWrap("Wallow in misery or follow your voice", 0.5f, Color.Gray, Vector2.UnitX * 50f);
			DrawLineWrap("The one that sang happiness, hope, and of joy", 0.5f, Color.Gray, Vector2.UnitX * 50f);
			DrawLineWrap("Striving to heal, and not self-destroy", 0.5f, Color.Gray, Vector2.UnitX * 50f);
		}

	}
}