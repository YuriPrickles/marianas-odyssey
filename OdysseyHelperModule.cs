using Monocle;
using System;
using Celeste;
using static Celeste.SaveData;
using Microsoft.Xna.Framework;
using System.IO;
using System.Diagnostics;
using System.Linq;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Celeste.Mod.OdysseyHelper;

public class OdysseyHelperModule : EverestModule {
	public static OdysseyHelperModule Instance { get; private set; }

	public override Type SettingsType => typeof(OdysseyHelperModuleSettings);
	public static OdysseyHelperModuleSettings Settings => (OdysseyHelperModuleSettings) Instance._Settings;

	public override Type SessionType => typeof(OdysseyHelperModuleSession);
	public static OdysseyHelperModuleSession Session => (OdysseyHelperModuleSession) Instance._Session;

	public override Type SaveDataType => typeof(OdysseyHelperModuleSaveData);
	public static OdysseyHelperModuleSaveData Desert_SaveData => (OdysseyHelperModuleSaveData) Instance._SaveData;

	public OdysseyHelperModule() {
		Instance = this;
#if DEBUG
		// debug builds use verbose logging
		Logger.SetLogLevel(nameof(OdysseyHelperModule), LogLevel.Verbose);
#else
		// release builds use info logging to reduce spam in log files
		Logger.SetLogLevel(nameof(OdysseyHelperModule), LogLevel.Info);
#endif
	}
	SpriteBank sprBankAbby;
	MoonParticle3D johnPork;
	public bool makeFinalBossFieldsInvisible = true;


	public override void LoadContent(bool firstLoad)
	{
		base.LoadContent(firstLoad);
		sprBankAbby = new SpriteBank(GFX.Game, Path.Combine("Graphics", "OdysseyOfSand", "NotForMetadataSprites.xml"));

	}
	public override void Load() {
        On.Celeste.Postcard.BeforeRender += Postcard_BeforeRender;
        On.Celeste.FinalBossStarfield.Update += FinalBossStarfield_Update;
		On.Celeste.BadelineDummy.Update += BadelineDummy_ctor;
		On.Celeste.MountainResources.ctor += MountainResources_ctor;
		On.Celeste.MountainModel.Update += MountainModel_Update;
		On.Celeste.MountainModel.ctor += MountainModel_ctor;
		On.Celeste.BadelineOldsite.Added += BadelineOldsite_Added;
		On.Celeste.BadelineOldsite.Update += BadelineOldsite_Update;
		On.Celeste.Checkpoint.Added += Checkpoint_Added;
		On.Celeste.Player.Die += Player_Die;
		On.Celeste.Player.DreamDashBegin += Player_DreamDashBegin;
		On.Celeste.Player.DreamDashEnd += Player_DreamDashEnd;
		// TODO: apply any hooks that should always be active

	}

	public override void Unload()
    {
        On.Celeste.Postcard.BeforeRender -= Postcard_BeforeRender;
        On.Celeste.FinalBossStarfield.Update -= FinalBossStarfield_Update;
        On.Celeste.BadelineDummy.Update -= BadelineDummy_ctor;
		On.Celeste.MountainResources.ctor -= MountainResources_ctor;
		On.Celeste.MountainModel.Update -= MountainModel_Update;
		On.Celeste.MountainModel.ctor -= MountainModel_ctor;
		On.Celeste.BadelineOldsite.Added -= BadelineOldsite_Added;
		On.Celeste.BadelineOldsite.Update -= BadelineOldsite_Update;
		On.Celeste.Checkpoint.Added -= Checkpoint_Added;
		On.Celeste.Player.Die -= Player_Die;
		On.Celeste.Player.DreamDashBegin -= Player_DreamDashBegin;
		On.Celeste.Player.DreamDashEnd -= Player_DreamDashEnd;
        // TODO: unapply any hooks applied in Load()
    }

    private void Postcard_BeforeRender(On.Celeste.Postcard.orig_BeforeRender orig, Postcard self)
    {
        string text = Dialog.Clean("FILE_DEFAULT");
        if (SaveData.Instance.LastArea_Safe.SID.Contains("Prickles/OdysseyOfSand"))
        {
            text = Dialog.Clean("MARIANA");
        }
		else
		{
			orig(self);
			return;
		}
		//rest of the original code
        if (self.target == null)
        {
            self.target = VirtualContent.CreateRenderTarget("postcard", self.postcard.Width, self.postcard.Height);
        }

        Engine.Graphics.GraphicsDevice.SetRenderTarget(self.target);
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
        Draw.SpriteBatch.Begin();
        if (SaveData.Instance != null && Dialog.Language.CanDisplay(SaveData.Instance.Name))
        {
            text = SaveData.Instance.Name;
        }

        self.postcard.Draw(Vector2.Zero);
        ActiveFont.Draw(text, new Vector2(115f, 30f), Vector2.Zero, Vector2.One * 0.9f, Color.Black * 0.7f);
        self.text.DrawJustifyPerLine(new Vector2(self.postcard.Width, self.postcard.Height) / 2f + new Vector2(0f, 40f), new Vector2(0.5f, 0.5f), Vector2.One * 0.7f, 1f);
        Draw.SpriteBatch.End();
    }

    private void FinalBossStarfield_Update(On.Celeste.FinalBossStarfield.orig_Update orig, FinalBossStarfield self, Scene scene)
    {
        orig(self, scene);
        if (makeFinalBossFieldsInvisible && (scene as Level).Session.Area.SID.Contains("Prickles/OdysseyOfSand"))
            self.Alpha = 0;
    }

    private void BadelineDummy_ctor(On.Celeste.BadelineDummy.orig_Update orig, BadelineDummy self)
    {
        orig(self);
        if (self.SceneAs<Level>().Session.Area.SID.Contains("Prickles/OdysseyOfSand"))
            if (self.Hair.Color == Color.White)
                return;
        self.Hair.Color = Color.White;
    }
    private void MountainResources_ctor(On.Celeste.MountainResources.orig_ctor orig, MountainResources self)
	{
		orig(self);
		self.MountainStates = new MountainState[5];
	}
	private void MountainModel_Update(On.Celeste.MountainModel.orig_Update orig, MountainModel self)
	{
		orig(self);
		if (SaveData.Instance?.LastArea_Safe.GetSID().Contains("Prickles/OdysseyOfSand") != null)
		{
			if (johnPork != null)
				Engine.Scene.Remove(johnPork);
			return;
		}
		else
		{
			self.NearFogAlpha = 1;
			if ((Engine.Scene as Overworld)?.Entities.OfType<MoonParticle3D>().First() == null)
			{
				Engine.Scene.Add(johnPork = new MoonParticle3D(self, new Vector3(0, 10000000, 0)));
			}
			self.StarEase = 0;
			self.ignoreCameraRotation = false;
		}
	}

	private void MountainModel_ctor(On.Celeste.MountainModel.orig_ctor orig, MountainModel self)
	{
		orig(self);
		Array.Resize(ref self.mountainStates, 5);
		Array.Resize(ref MTN.MountainTerrainTextures, 5);
		Array.Resize(ref MTN.MountainBuildingTextures, 5);
		Array.Resize(ref MTN.MountainSkyboxTextures, 5);

		MTN.MountainSkyboxTextures[4] = MTN.Mountain["Prickles/OdysseyOfSand/skybox_" + 4].Texture;
		MTN.MountainTerrainTextures[4] = MTN.Mountain["Prickles/OdysseyOfSand/mountain_" + 4].Texture;
		MTN.MountainBuildingTextures[4] = MTN.Mountain["Prickles/OdysseyOfSand/buildings_" + 4].Texture;
		self.mountainStates[4] = new MountainState(MTN.MountainTerrainTextures[4], MTN.MountainBuildingTextures[4], MTN.MountainSkyboxTextures[4], Calc.HexToColor("ae5433"));
	}

	private void BadelineOldsite_Added(On.Celeste.BadelineOldsite.orig_Added orig, BadelineOldsite self, Scene scene)
	{
		Sprite trueSprite;
		orig(self, scene);
		if ((scene as Level).Session.Level == "2a-lake" && (scene as Level).Session.Area.LevelSet != "Prickles/OdysseyOfSand") return;
		self.Remove(self.Hair, self.Sprite);
		Session session = self.SceneAs<Level>().Session;
		self.Add(trueSprite = sprBankAbby.Create("abby_demon"));
		trueSprite.Visible = true;
		trueSprite.Play("idle");
	}

	private void BadelineOldsite_Update(On.Celeste.BadelineOldsite.orig_Update orig, BadelineOldsite self)
	{
		Sprite trueSprite = new Sprite();
		foreach (Component component in self.Components)
		{
			if (component is Sprite && component is not PlayerSprite)
			{
				trueSprite = component as Sprite;
			}
		}
		orig(self);
		if ((self.Scene as Level).Session.Level == "2a-lake" && (self.Scene as Level).Session.Area.LevelSet != "Prickles/OdysseyOfSand") return;
		self.Hovering = true;
		self.Depth = -15000;
		trueSprite.Scale.X = self.Sprite.Scale.X;
		if (self.Hovering)
		{
			self.hoveringTimer += Engine.DeltaTime;
			trueSprite.Y = (float)(Math.Sin(self.hoveringTimer * 2f) * 4.0) + 2;
		}
		else
		{
			trueSprite.Y = Calc.Approach(self.Sprite.Y, 0f, Engine.DeltaTime * 4f) + 2;
		}
	}

	private PlayerDeadBody Player_Die(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats)
	{
		Level level = (self.Scene as Level);
		if (level.Session.Audio.Music.Event != "event:/Prickles/OdysseyOfSand/merryana_sunsetfight" && level.Session.Area.LevelSet == "Prickles/OdysseyOfSand" && level.Session.Area.SID == "Prickles/OdysseyOfSand/3-MirageGrove")
			{ //Audio.SetMusicParam("boss_pitch", 0f);
			}
		return orig(self,direction,evenIfInvincible,registerDeathInStats);
	}
	private void Player_DreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player self)
	{
		orig(self);
		Level level = (self.Scene as Level);
		if (level.Session.Audio.Music.Event != "event:/Prickles/OdysseyOfSand/merryana_sunsetfight" && level.Session.Area.LevelSet == "Prickles/OdysseyOfSand" && level.Session.Area.SID == "Prickles/OdysseyOfSand/3-MirageGrove")
			{// Audio.SetMusicParam("boss_pitch", 1f); 
		}
	}

	private void Player_DreamDashEnd(On.Celeste.Player.orig_DreamDashEnd orig, Player self)
	{
		orig(self);
		Level level = (self.Scene as Level);
        if (level.Session.Audio.Music.Event != "event:/Prickles/OdysseyOfSand/merryana_sunsetfight" && level.Session.Area.LevelSet == "Prickles/OdysseyOfSand" && level.Session.Area.SID == "Prickles/OdysseyOfSand/3-MirageGrove")
        { //Audio.SetMusicParam("boss_pitch", 0f);
        }
    }


	private void Checkpoint_Added(On.Celeste.Checkpoint.orig_Added orig, Checkpoint self, Scene scene)
	{
		self.Scene = scene;
		if ((scene as Level).Session.Area.LevelSet == "Prickles/OdysseyOfSand")
		{
			Level level = (scene as Level);
			string checkpointType = "sun";
			string imagePath = "";
			SpriteBank sprBank = new SpriteBank(GFX.Game, Path.Combine("Graphics", "OdysseyOfSand", "NotForMetadataSprites.xml"));
			switch (level.Session.Area.SID)
			{
				case "Prickles/OdysseyOfSand/1-TheEmptiness":
				case "Prickles/OdysseyOfSand/4-TerracottaTowers":
				case "Prickles/OdysseyOfSand/5-SuneaterPlateau":
					imagePath = "objects/checkpoint/Prickles/OdysseyOfSand/cliffsideBG";
					checkpointType = "sun";
					break;
				case "Prickles/OdysseyOfSand/2-SpiritTemple":
					imagePath = "objects/checkpoint/Prickles/OdysseyOfSand/coreBG";
					checkpointType = "sun";
					break;
				case "Prickles/OdysseyOfSand/3-MirageGrove":
					imagePath = "objects/checkpoint/Prickles/OdysseyOfSand/ReflBG_moon";
					checkpointType = "moon";
					break;
				case "Prickles/OdysseyOfSand/6-DesertDescent":
					switch (level.Session.Level)
					{
						case "2a":
						case "5a":
						case "6a":
							imagePath = "objects/checkpoint/Prickles/OdysseyOfSand/cliffsideBG_moon";
							break;
						case "4a":
							imagePath = "objects/checkpoint/Prickles/OdysseyOfSand/coreBG_moon";
							break;
						case "3a":
							imagePath = "objects/checkpoint/Prickles/OdysseyOfSand/ReflBG_moon";
							break;
					}
					checkpointType = "moon";
					break;
			}
			self.Add(self.image = new Image(GFX.Game[imagePath]));
			self.image.JustifyOrigin(0.5f, 1f);
			if (GFX.Game.Has(imagePath))
			{
			}

			self.Add(self.sprite = sprBank.Create("checkpoint_highlight_" + checkpointType));
			self.sprite.Play("off");
			self.Add(self.flash = sprBank.Create("checkpoint_flash_" + checkpointType));
			self.flash.Visible = false;
			self.flash.Color = Color.White * 0.6f;
			if (SaveData.Instance.HasCheckpoint(level.Session.Area, level.Session.Level))
			{
				self.TurnOn(animate: false);
			}
			return;
		}
		orig(self, scene);
	}

}