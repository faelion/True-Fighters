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
            world.EnqueueEvent(new DashEvent
            {
                SourceId = id,
                CasterId = playerId,
                PosX = caster.Transform.posX,
                PosY = caster.Transform.posY,
                DirX = dir.x,
                DirY = dir.y,
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
