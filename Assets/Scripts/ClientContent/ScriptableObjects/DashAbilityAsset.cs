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
            var caster = world.EnsurePlayer(playerId);
            float dx = targetX - caster.posX;
            float dy = targetY - caster.posY;
            float dist2 = dx * dx + dy * dy;
            if (dist2 > range * range) return false;

            Shared.MathUtil.Normalize(ref dx, ref dy);
            world.EnqueueAbilityEvent(new AbilityEventMessage
            {
                abilityIdOrKey = id,
                casterId = playerId,
                eventType = AbilityEventType.Dash,
                posX = caster.posX,
                posY = caster.posY,
                dirX = dx,
                dirY = dy,
                speed = speed,
                serverTick = System.Environment.TickCount
            });
            return true;
        }

        public override void ClientHandleEvent(AbilityEventMessage evt, GameObject contextRoot)
        {
            if (evt.abilityIdOrKey != id) return;
            if (evt.eventType != AbilityEventType.Dash) return;
            GameObject prefab = dashVfx;
            if (!prefab) return;
            var go = Object.Instantiate(prefab, new Vector3(evt.posX, 0f, evt.posY), Quaternion.LookRotation(new Vector3(evt.dirX, 0f, evt.dirY)));
            SceneManager.MoveGameObjectToScene(go, contextRoot.scene);
            Object.Destroy(go, 1.0f);
        }
    }
}
