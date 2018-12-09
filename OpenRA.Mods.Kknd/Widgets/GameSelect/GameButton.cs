using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets.GameSelect
{
    public class GameButtonWidget : Widget
    {
        private GameSelectWidget gameSelect;

        private Sprite inactive;
        private Sprite active;
        
        private bool isHovered;
        private bool isActive;

        private string game;

        public GameButtonWidget(string game, int index, GameSelectWidget gameSelect)
        {
            this.game = game;
            this.gameSelect = gameSelect;

            Bounds = new Rectangle(index * 512, 62, 512, 706);

            inactive = new Sprite(new Sheet(SheetType.BGRA, Game.ModData.ModFiles.Open("uibits/" + game + ".png")), new Rectangle(0, 0, 512, 706), TextureChannel.RGBA);
            active = new Sprite(new Sheet(SheetType.BGRA, Game.ModData.ModFiles.Open("uibits/" + game + "_active.png")), new Rectangle(0, 0, 512, 706), TextureChannel.RGBA);
        }

        public override void MouseEntered()
        {
            isHovered = true;
        }

        public override void MouseExited()
        {
            isHovered = false;
        }

        public override bool HandleMouseInput(MouseInput mi)
        {
            if (mi.Button == MouseButton.Left)
            {
                foreach (var widget in Parent.Children.Where(child => child is GameButtonWidget))
                    ((GameButtonWidget) widget).isActive = widget == this;
                isActive = true;
                gameSelect.Game = game;
                gameSelect.State = 1;
                return true;
            }

            return false;
        }

        public override void Draw()
        {
            WidgetUtils.DrawRGBA(isHovered || isActive ? active : inactive, new float2(RenderBounds.X, RenderBounds.Y));
        }
    }
}
