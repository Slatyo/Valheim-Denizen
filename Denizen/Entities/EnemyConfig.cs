using Denizen.Core;

namespace Denizen.Entities
{
    /// <summary>
    /// Configuration for enemy entities.
    /// </summary>
    public class EnemyConfig : DenizenConfig
    {
        public EnemyConfig()
        {
            Type = DenizenType.Enemy;
            Behavior = new BehaviorConfig
            {
                Aggression = Aggression.Aggressive,
                GroupBehavior = GroupBehavior.Pack
            };
        }
    }
}
