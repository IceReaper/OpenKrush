using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Behavior
{
    [Desc("Makes specific actors ignore the rally point when created.")]
    public class IgnoreRallyPointInfo : ITraitInfo
    {
        public object Create(ActorInitializer init) { return new IgnoreRallyPoint(); }
    }

    public class IgnoreRallyPoint : INotifyCreated
    {
        void INotifyCreated.Created(Actor self)
        {
            self.World.AddFrameEndTask(world =>
            {
                var activity = self.CurrentActivity;

                while (activity != null && !(activity is AttackMoveActivity))
                    activity = activity.NextActivity;

                if (activity != null)
                    activity.Cancel(self, true);
            });
        }
    }
}
