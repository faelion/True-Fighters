using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability Assets/Dash", fileName = "DashAbilityAsset")]
    public class DashAbilityAsset : AbilityAsset
    {
        [Header("Effects")]
        public System.Collections.Generic.List<Shared.Effects.Effect> Effects;

        [Header("View")]
        public GameObject dashVfx;

        public override bool ServerTryCast(ServerGame.ServerWorld world, int playerId, float targetX, float targetY)
        {
            if (!ValidateCastRange(world, playerId, targetX, targetY, out var dir)) return false;
            
            var caster = world.EnsurePlayer(playerId);
            if (!caster.TryGetComponent(out ServerGame.Entities.TransformComponent t)) return false;
            
            // Rotate caster to face dash direction
            //t.rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Apply Self Effects (Dash, Buffs, etc)
            if (Effects != null)
            {
                foreach (var effect in Effects)
                {
                    if (effect != null)
                    {
                        effect.Apply(world, caster, caster); // Self-cast
                    }
                }
            }

            return true;
        }

        public override void ClientHandleEvent(IGameEvent evt, GameObject contextRoot)
        {
        }
    }
}
