using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Kknd.Widgets.Ingame.Buttons
{
	public sealed class BomberButtonWidget : ButtonWidget
	{
		public BomberButtonWidget(SidebarWidget sidebar) : base(sidebar, "button")
		{
			TooltipTitle = "Aircrafts";
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (!IsUsable() || e.IsRepeat || e.Event != KeyInputEvent.Down)
				return false;

			if (!Active && e.Key == Game.ModData.Hotkeys["Superweapons"].GetValue().Key && e.Modifiers == Game.ModData.Hotkeys["Superweapons"].GetValue().Modifiers)
			{
				Active = true;
				sidebar.CloseAllBut(this);
				return true;
			}

			if (Active)
			{
				var lastItem = Math.Min(12, Children.Count);

				for (var i = 0; i < lastItem; i++)
				{
					if (e.Key != Game.ModData.Hotkeys["Production" + (i + 1)].GetValue().Key || e.Modifiers != Game.ModData.Hotkeys["Production" + (i + 1)].GetValue().Modifiers)
						continue;

					((ProductionItemButtonWidget)Children[i]).ClickedLeft(new MouseInput(MouseInputEvent.Down, MouseButton.None, 0, int2.Zero, e.Modifiers, 0));

					return true;
				}
			}

			return false;
		}

		protected override bool HandleLeftClick(MouseInput mi)
		{
			if (base.HandleLeftClick(mi))
			{
				if (Active)
					sidebar.CloseAllBut(this);

				return true;
			}

			return false;
		}

		protected override bool IsUsable()
		{
			return Children.Count > 0;
		}

		public override void Tick()
		{
			var spm = sidebar.IngameUi.World.LocalPlayer.PlayerActor.Trait<SupportPowerManager>();
			var powers = spm.Powers.Values.Where(p => !p.Disabled).ToArray();

			var oldButtons = Children.Where(c => powers.All(b => b.Key != ((ProductionItemButtonWidget)c).Item)).ToArray();

			foreach (var oldButton in oldButtons)
				Children.Remove(oldButton);

			for (var i = 0; i < powers.Length; i++)
			{
				var power = powers[i];

				var button = Children.FirstOrDefault(c =>
				{
					var widget = c as ProductionItemButtonWidget;
					return widget != null && widget.Item == power.Key;
				});

				if (button == null)
				{
					button = new ProductionItemButtonWidget(sidebar)
					{
						Item = power.Info.Icon,
						Icon = power.Info.Icon,
						Progress = () => power.Ready ? -1 : (power.TotalTime - power.RemainingTime) * 100 / power.TotalTime,
						Amount = () => 0,
						ClickedLeft = mi => power.Target(),
						ClickedRight = null,
						IsActive = () =>
						{
							var og = sidebar.IngameUi.World.OrderGenerator as SelectGenericPowerTarget;
							return og != null && og.OrderKey == power.Key;
						},
						IsFocused = () => false,
						TooltipTitle = power.Info.Description,
						TooltipText = null
					};

					AddChild(button);
				}

				button.Visible = Active;
				button.Bounds.X = (-1 - i) * Size;
			}

			if (Children.Count == 0)
				Active = false;

			type = Children.Count > 0 ? "unit" : "button";
		}

		protected override void DrawContents()
		{
			sidebar.Buttons.PlayFetchIndex("aircraft", () => 0);
			WidgetUtils.DrawSHPCentered(sidebar.Buttons.Image, center + new int2(0, Active ? 1 : 0), sidebar.IngameUi.Palette);
		}
	}
}
