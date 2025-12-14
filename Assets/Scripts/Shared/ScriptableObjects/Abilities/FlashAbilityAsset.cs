using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability Assets/Flash", fileName = "FlashAbilityAsset")]
    public class FlashAbilityAsset : AbilityAsset
    {
        [Header("Effects")]
        public System.Collections.Generic.List<Shared.Effects.Effect> Effects;

        [Header("View")]
        public GameObject FlashVfx;
        public float vfxDuration = 0.2f;

        public override bool ServerTryCast(ServerGame.ServerWorld world, int playerId, float targetX, float targetY)
        {
            if (!ValidateCastRange(world, playerId, targetX, targetY, out var dir)) return false;
            
            var caster = world.EnsurePlayer(playerId);
            if (!caster.TryGetComponent(out ServerGame.Entities.TransformComponent t)) return false;

            float dx = targetX - t.posX;
            float dy = targetY - t.posY;
            float distSq = dx * dx + dy * dy;

            float dist = Mathf.Sqrt(distSq);
            float nx = dx / dist;
            float ny = dy / dist;

            float angle = Mathf.Atan2(nx, ny) * Mathf.Rad2Deg;
            t.rotZ = angle;

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

        public override void ClientOnCast(AbilityCastedEvent evt, GameObject contextRoot)
        {
            if (FlashVfx != null && contextRoot != null)
            {
                var vfx = Instantiate(FlashVfx, contextRoot.transform.position, contextRoot.transform.rotation);
                Destroy(vfx, vfxDuration); 
            }
            Debug.Log($"[FlashAbilityAsset] ClientOnCast executed on {contextRoot?.name}");
        }
    }
}
