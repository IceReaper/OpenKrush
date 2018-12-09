using System.Collections.Generic;
using OpenRA.Mods.Kknd.Widgets.GameSelect;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.LoadScreens
{
    public class GameSelectLoadScreen : ILoadScreen
    {
        public void Dispose()
        {
        }

        public void Init(ModData m, Dictionary<string, string> info)
        {
        }

        public void Display()
        {
        }

        public bool BeforeLoad()
        {
            return true;
        }

        public void StartGame(Arguments args)
        {
            Ui.Root.AddChild(new GameSelectWidget());
        }
    }
}
