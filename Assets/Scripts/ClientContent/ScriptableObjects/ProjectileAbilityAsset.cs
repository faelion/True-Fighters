using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability Assets/Projectile", fileName = "ProjectileAbilityAsset")]
    public class ProjectileAbilityAsset : AbilityAsset
    {
        [System.Serializable]
        public class ProjectileEffectData : ServerGame.AbilityEffectData
        {
            public float damage;
            public bool spawnSent;
        }

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

            Vector2 dir = new Vector2(dx, dy);
            if (dir.sqrMagnitude <= 0.0001f) dir = Vector2.right;
            dir.Normalize();
            var effect = new ServerGame.AbilityEffect
            {
                ownerPlayerId = playerId,
                abilityId = id,
                posX = caster.posX,
                posY = caster.posY,
                dirX = dir.x,
                dirY = dir.y,
                speed = projectileSpeed,
                lifeMs = projectileLifeMs,
                data = new ProjectileEffectData { damage = damage, spawnSent = false }
            };
            int projId = world.RegisterAbilityEffect(effect, this);
            return true;
        }

        public override bool OnEffectTick(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, float dt)
        {
            // Move projectile
            float step = eff.speed * dt;
            eff.posX += eff.dirX * step;
            eff.posY += eff.dirY * step;
            
            var data = eff.GetData<ProjectileEffectData>();

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
                    OnEffectHit(world, eff, p.playerId);
                    // data?.damage could be used here to reduce HP when HealthSystem exista
                    eff.lifeMs = 0; // mark for despawn
                    break;
                }
            }
            return eff.lifeMs > 0;
        }

        public override void EmitEvents(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, int tick, System.Collections.Generic.IList<IGameEvent> buffer)
        {
            var data = eff.GetData<ProjectileEffectData>();
            if (data != null && !data.spawnSent)
            {
                buffer.Add(new ProjectileSpawnEvent
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
                });
                data.spawnSent = true;
            }

            buffer.Add(new ProjectileUpdateEvent
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
            });
        }

        public override bool EmitDespawnEvent(ServerGame.ServerWorld world, ServerGame.AbilityEffect effect, out IGameEvent evt)
        {
            evt = new ProjectileDespawnEvent
            {
                SourceId = id,
                CasterId = 0,
                ProjectileId = effect.id
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
