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
            world.EnqueueEvent(new DashEvent
            {
                SourceId = id,
                CasterId = playerId,
                PosX = caster.posX,
                PosY = caster.posY,
                DirX = dx,
                DirY = dy,
                Speed = speed,
                ServerTick = 0
            });
            return true;
        }

        public override void ClientHandleEvent(IGameEvent evt, GameObject contextRoot)
        {
            if (evt == null || evt.Type != GameEventType.Dash || evt.SourceId != id) return;
            var dash = (DashEvent)evt;
            GameObject prefab = dashVfx;
            if (!prefab) return;
            var go = Object.Instantiate(prefab, new Vector3(dash.PosX, 0f, dash.PosY), Quaternion.LookRotation(new Vector3(dash.DirX, 0f, dash.DirY)));
            SceneManager.MoveGameObjectToScene(go, contextRoot.scene);
            Object.Destroy(go, 1.0f);
        }
    }
}
