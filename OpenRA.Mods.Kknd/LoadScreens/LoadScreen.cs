using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.LoadScreens;

namespace OpenRA.Mods.Kknd.LoadScreens
{
	public class LoadScreen : BlankLoadScreen
	{
		Renderer renderer;
		Sheet sheet1;
		Sheet sheet2;
		Sprite logo1;
		Sprite logo2;
		bool started;

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			base.Init(modData, info);

			renderer = Game.Renderer;
			if (renderer == null)
				return;

			sheet1 = new Sheet(SheetType.BGRA, modData.DefaultFileSystem.Open("uibits/loading_game.png"));
			sheet2 = new Sheet(SheetType.BGRA, modData.DefaultFileSystem.Open("uibits/loading_map.png"));
			logo1 = new Sprite(sheet1, new Rectangle(0, 0, 640, 480), TextureChannel.RGBA);
			logo2 = new Sprite(sheet2, new Rectangle(0, 0, 640, 480), TextureChannel.RGBA);
		}

		public override void StartGame(Arguments args)
		{
			// TODO if !started, add VBC playback here! (and if it works, remove it from the modloader!
			base.StartGame(args);
			started = true;
		}

		public override void Display()
		{
			if (renderer == null)
				return;

			var logoPos = new float2(renderer.Resolution.Width / 2 - 320, renderer.Resolution.Height / 2 - 240);

			renderer.BeginFrame(int2.Zero, 1f);
			renderer.RgbaSpriteRenderer.DrawSprite(started ? logo2 : logo1, logoPos);
			renderer.EndFrame(new NullInputHandler());
		}

		protected override void Dispose(bool disposing)
		{
			sheet1.Dispose();
			sheet2.Dispose();
			base.Dispose(disposing);
		}
	}
}
