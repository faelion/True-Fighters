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

        public override bool ServerPopulateSpawnEvent(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, int tick, out IGameEvent evt)
        {
            evt = new ProjectileSpawnEvent
            {
                SourceId = id,
                CasterId = eff.ownerPlayerId,
                ServerTick = tick,
                ProjectileId = eff.id,
                PosX = eff.posX,
                PosY = eff.posY,
                DirX = eff.dirX,
                DirY = eff.dirY,
                Speed = eff.speed,
                LifeMs = eff.lifeMs
            };
            return true;
        }

        public override bool ServerPopulateUpdateEvent(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, int tick, out IGameEvent evt)
        {
            evt = new ProjectileUpdateEvent
            {
                SourceId = id,
                CasterId = eff.ownerPlayerId,
                ServerTick = tick,
                ProjectileId = eff.id,
                PosX = eff.posX,
                PosY = eff.posY,
                DirX = eff.dirX,
                DirY = eff.dirY,
                Speed = eff.speed,
                LifeMs = eff.lifeMs
            };
            return true;
        }

        public override bool ServerPopulateDespawnEvent(ServerGame.ServerWorld world, int effectId, int tick, out IGameEvent evt)
        {
            evt = new ProjectileDespawnEvent
            {
                SourceId = id,
                CasterId = 0,
                ServerTick = tick,
                ProjectileId = effectId
            };
            return true;
        }

        private readonly System.Collections.Generic.Dictionary<int, GameObject> live = new System.Collections.Generic.Dictionary<int, GameObject>();

        public override void ClientHandleEvent(IGameEvent evt, GameObject contextRoot)
        {
            if (evt == null || evt.SourceId != id) return;
            switch (evt.Type)
            {
                case GameEventType.ProjectileSpawn:
                {
                    var e = (ProjectileSpawnEvent)evt;
                    if (live.ContainsKey(e.ProjectileId)) break;
                    GameObject prefab = projectilePrefab;
                    GameObject go;
                    if (prefab)
                        go = Object.Instantiate(prefab, new Vector3(e.PosX, 0f, e.PosY), Quaternion.identity);
                    else
                    {
                        go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.transform.position = new Vector3(e.PosX, 0f, e.PosY);
                        go.transform.localScale = Vector3.one * 0.3f;
                    }
                    SceneManager.MoveGameObjectToScene(go, contextRoot.scene);
                    float angle = Mathf.Atan2(e.DirY, e.DirX) * Mathf.Rad2Deg;
                    go.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                    live[e.ProjectileId] = go;
                    break;
                }
                case GameEventType.ProjectileUpdate:
                {
                    var e = (ProjectileUpdateEvent)evt;
                    if (live.TryGetValue(e.ProjectileId, out var uGo) && uGo)
                    {
                        uGo.transform.position = new Vector3(e.PosX, 0f, e.PosY);
                        if (e.LifeMs <= 0)
                        {
                            Object.Destroy(uGo);
                            live.Remove(e.ProjectileId);
                        }
                    }
                    break;
                }
                case GameEventType.ProjectileDespawn:
                {
                    var e = (ProjectileDespawnEvent)evt;
                    if (live.TryGetValue(e.ProjectileId, out var dGo))
                    {
                        if (dGo) Object.Destroy(dGo);
                        live.Remove(e.ProjectileId);
                    }
                    break;
                }
            }
        }
    }
}
