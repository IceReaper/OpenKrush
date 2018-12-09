using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Projectiles
{
    [Desc("Explodes on the attacker position instead of the target position.")]
    public class SourceExplosionInfo : IProjectileInfo
    {
        public IProjectile Create(ProjectileArgs args) { return new SourceExplosion(args); }
    }

    public class SourceExplosion : IProjectile
    {
        public SourceExplosion(ProjectileArgs args)
        {
            args.Weapon.Impact(Target.FromPos(args.SourceActor.CenterPosition), args.SourceActor, args.DamageModifiers);
        }

        public void Tick(World world) { }

        public IEnumerable<IRenderable> Render(WorldRenderer r) { return new IRenderable[0]; }
    }
}
