using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability Assets/Projectile", fileName = "ProjectileAbilityAsset")]
    public class ProjectileAbilityAsset : AbilityAsset
    {
        [Header("Projectile Data")]
        public float projectileSpeed = 8f;
        public int projectileLifeMs = 1500;
        public float damage = 0f;

        [Header("View")]
        public GameObject projectilePrefab;

        public override bool ServerTryCast(ServerGame.ServerWorld world, int playerId, float targetX, float targetY)
        {
            var caster = world.EnsurePlayer(playerId);
            float dx = targetX - caster.posX;
            float dy = targetY - caster.posY;
            float dist2 = dx * dx + dy * dy;
            if (dist2 > range * range) return false;

            float dirX = dx, dirY = dy;
            Shared.MathUtil.Normalize(ref dirX, ref dirY);
            var effect = new ServerGame.AbilityEffect
            {
                ownerPlayerId = playerId,
                abilityId = id,
                posX = caster.posX,
                posY = caster.posY,
                dirX = dirX,
                dirY = dirY,
                speed = projectileSpeed,
                lifeMs = projectileLifeMs
            };
            int projId = world.RegisterAbilityEffect(effect, markSpawn: true);
            return true;
        }

        public override bool ServerUpdateEffect(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, float dt)
        {
            // Move projectile
            float step = eff.speed * dt;
            eff.posX += eff.dirX * step;
            eff.posY += eff.dirY * step;
            
            // Simple collision with players different from owner
            foreach (var p in world.Players.Values)
            {
                if (p.playerId == eff.ownerPlayerId) continue;
                float dx = p.posX - eff.posX;
                float dy = p.posY - eff.posY;
                float dist2 = dx * dx + dy * dy;
                if (dist2 < 0.25f)
                {
                    p.hit = true;
                    eff.lifeMs = 0; // mark for despawn
                    break;
                }
            }
            return eff.lifeMs > 0;
        }

        public override bool ServerPopulateSpawnEvent(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, int tick, AbilityEventMessage msg)
        {
            msg.abilityIdOrKey = id;
            msg.casterId = eff.ownerPlayerId;
            msg.eventType = AbilityEventType.SpawnProjectile;
            msg.projectileId = eff.id;
            msg.posX = eff.posX; msg.posY = eff.posY;
            msg.dirX = eff.dirX; msg.dirY = eff.dirY;
            msg.speed = eff.speed; msg.lifeMs = eff.lifeMs;
            msg.serverTick = tick;
            return true;
        }

        public override bool ServerPopulateUpdateEvent(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, int tick, AbilityEventMessage msg)
        {
            msg.abilityIdOrKey = id;
            msg.casterId = eff.ownerPlayerId;
            msg.eventType = AbilityEventType.ProjectileUpdate;
            msg.projectileId = eff.id;
            msg.posX = eff.posX; msg.posY = eff.posY;
            msg.dirX = eff.dirX; msg.dirY = eff.dirY;
            msg.speed = eff.speed; msg.lifeMs = eff.lifeMs;
            msg.serverTick = tick;
            return true;
        }

        public override bool ServerPopulateDespawnEvent(ServerGame.ServerWorld world, int effectId, int tick, AbilityEventMessage msg)
        {
            msg.abilityIdOrKey = id;
            msg.casterId = 0;
            msg.eventType = AbilityEventType.ProjectileDespawn;
            msg.projectileId = effectId;
            msg.serverTick = tick;
            return true;
        }

        private readonly System.Collections.Generic.Dictionary<int, GameObject> live = new System.Collections.Generic.Dictionary<int, GameObject>();

        public override void ClientHandleEvent(AbilityEventMessage evt, GameObject contextRoot)
        {
            if (evt.abilityIdOrKey != id) return;
            switch (evt.eventType)
            {
                case AbilityEventType.SpawnProjectile:
                    if (live.ContainsKey(evt.projectileId)) break;
                    GameObject prefab = projectilePrefab;
                    GameObject go;
                    if (prefab)
                        go = Object.Instantiate(prefab, new Vector3(evt.posX, 0f, evt.posY), Quaternion.identity);
                    else
                    {
                        go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.transform.position = new Vector3(evt.posX, 0f, evt.posY);
                        go.transform.localScale = Vector3.one * 0.3f;
                    }
                    SceneManager.MoveGameObjectToScene(go, contextRoot.scene);
                    float angle = Mathf.Atan2(evt.dirY, evt.dirX) * Mathf.Rad2Deg;
                    go.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                    live[evt.projectileId] = go;
                    break;
                case AbilityEventType.ProjectileUpdate:
                    if (live.TryGetValue(evt.projectileId, out var uGo) && uGo)
                    {
                        uGo.transform.position = new Vector3(evt.posX, 0f, evt.posY);
                        if (evt.lifeMs <= 0)
                        {
                            Object.Destroy(uGo);
                            live.Remove(evt.projectileId);
                        }
                    }
                    break;
                case AbilityEventType.ProjectileDespawn:
                    if (live.TryGetValue(evt.projectileId, out var dGo))
                    {
                        if (dGo) Object.Destroy(dGo);
                        live.Remove(evt.projectileId);
                    }
                    break;
            }
        }
    }
}
