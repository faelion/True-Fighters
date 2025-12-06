using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability Assets/Dash", fileName = "DashAbilityAsset")]
    public class DashAbilityAsset : AbilityAsset
    {
        [Header("Dash Data")]
        public float distance = 4f;
        public float speed = 10f;
        public float damageOnHit = 0f;

        [Header("View")]
        public GameObject dashVfx;

        public override bool ServerTryCast(ServerGame.ServerWorld world, int playerId, float targetX, float targetY)
        {
            if (!ValidateCastRange(world, playerId, targetX, targetY, out var dir)) return false;
            var caster = world.EnsurePlayer(playerId);
            if (!caster.TryGetComponent(out ServerGame.Entities.TransformComponent t)) return false;
            // User requested removal of custom events.
            // visual effects for dash should be handled via State updates (e.g. high velocity) or EntitySpawn of a generic VF effect.
            // For now, removing the event.
            // world.EnqueueEvent(...)
            return true;
        }

        public override void ClientHandleEvent(IGameEvent evt, GameObject contextRoot)
        {
            // DashEvent removed.
            // Visuals to be handled by spotting velocity change or specific Component state if needed.
        }
    }
}
