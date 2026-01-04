using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability Assets/SelfCast", fileName = "SelfCastAbilityAsset")]
    public class SelfCastAbilityAsset : AbilityAsset
    {
        [Header("Effects")]
        public System.Collections.Generic.List<Shared.Effects.Effect> Effects;

        [Header("View")]
        public GameObject vfx;
        public float vfxDuration = 0.2f;

        public override bool ServerTryCast(ServerGame.ServerWorld world, int playerId, float targetX, float targetY)
        {
            if (!ValidateCastRange(world, playerId, targetX, targetY, out var dir)) return false;

            var caster = world.EnsurePlayer(playerId);

            // Apply Self Effects (Dash, Buffs, etc)
            if (Effects != null)
            {
                foreach (var effect in Effects)
                {
                    if (effect != null)
                    {
                        effect.Apply(world, caster, caster, new Vector3(targetX, 0, targetY)); // Self-cast with target param
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
            base.ClientOnCast(evt, contextRoot);

            if (vfx != null && contextRoot != null)
            {
                var vfx = Instantiate(this.vfx, contextRoot.transform.position, contextRoot.transform.rotation);
                Destroy(vfx, vfxDuration); 
            }
            Debug.Log($"[FlashAbilityAsset] ClientOnCast executed on {contextRoot?.name}");
        }
    }
}
