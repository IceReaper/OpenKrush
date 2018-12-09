using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Kknd.Traits.Altar;

namespace OpenRA.Mods.Kknd.Activities
{
	public class Sacrifice : Enter
	{
		private readonly Actor target;

		public Sacrifice(Actor self, Actor target) : base(self, target, EnterBehaviour.Dispose)
		{
			this.target = target;
		}

		protected override void OnInside(Actor self)
		{
			target.Trait<Altar>().Enter(self);
		}
	}
}
