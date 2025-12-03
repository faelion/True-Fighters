using System.Collections.Generic;
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


        private readonly Dictionary<int, GameObject> live = new Dictionary<int, GameObject>();

        public override bool ServerTryCast(ServerGame.ServerWorld world, int playerId, float targetX, float targetY)
        {
            if (!ValidateCastRange(world, playerId, targetX, targetY, out var dir)) return false;
            var caster = world.EnsurePlayer(playerId);
            var effect = new ServerGame.AbilityEffect
            {
                ownerPlayerId = playerId,
                abilityId = id,
                posX = caster.Transform.posX,
                posY = caster.Transform.posY,
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

            float step = eff.speed * dt;
            eff.posX += eff.dirX * step;
            eff.posY += eff.dirY * step;
            
            var data = eff.GetData<ProjectileEffectData>();


            world.TryGetEntity(eff.ownerPlayerId, out var casterEntity);
            foreach (var entity in world.EntityRepo.AllEntities)
            {
                if (entity.Id == eff.ownerPlayerId) continue;
                if (!entity.Health.IsAlive) continue;
                if (casterEntity != null && !casterEntity.Team.IsEnemyTo(entity.Team)) continue;
                float dx = entity.Transform.posX - eff.posX;
                float dy = entity.Transform.posY - eff.posY;
                float dist2 = dx * dx + dy * dy;
                if (dist2 < 0.25f)
                {
                    float dmg = data != null ? data.damage : damage;
                    entity.Health.ApplyDamage(dmg);
                    OnEffectHit(world, eff, entity.Id);
                    eff.lifeMs = 0;
                    break;
                }
            }
            return eff.lifeMs > 0;
        }

        public override void EmitEvents(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, int tick, System.Collections.Generic.IList<IGameEvent> buffer)
        {
            var data = eff.GetData<ProjectileEffectData>();
            var action = ProjectileAction.Update;

            if (data != null && !data.spawnSent)
            {
                action = ProjectileAction.Spawn;
                data.spawnSent = true;
            }

            buffer.Add(new ProjectileEvent
            {
                Action = action,
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
            evt = new ProjectileEvent
            {
                Action = ProjectileAction.Despawn,
                SourceId = id,
                CasterId = 0,
                ProjectileId = effect.id
            };
            return true;
        }

        public override void ClientHandleEvent(IGameEvent evt, GameObject contextRoot)
        {
            if (evt == null || evt.SourceId != id) return;
            if (evt.Type == GameEventType.Projectile)
            {
                var projectileEvent = (ProjectileEvent)evt;
                switch (projectileEvent.Action)
                {
                    case ProjectileAction.Spawn:
                    {
                        if (live.ContainsKey(projectileEvent.ProjectileId)) break;
                        GameObject prefab = projectilePrefab;
                        GameObject go;
                        if (prefab)
                            go = Object.Instantiate(prefab, new Vector3(projectileEvent.PosX, 0f, projectileEvent.PosY), Quaternion.identity);
                        else
                        {
                            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            go.transform.position = new Vector3(projectileEvent.PosX, 0f, projectileEvent.PosY);
                            go.transform.localScale = Vector3.one * 0.3f;
                        }
                        SceneManager.MoveGameObjectToScene(go, contextRoot.scene);
                        float angle = Mathf.Atan2(projectileEvent.DirY, projectileEvent.DirX) * Mathf.Rad2Deg;
                        go.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                        live[projectileEvent.ProjectileId] = go;
                        break;
                    }
                    case ProjectileAction.Update:
                    {
                        if (live.TryGetValue(projectileEvent.ProjectileId, out var uGo) && uGo)
                        {
                            uGo.transform.position = new Vector3(projectileEvent.PosX, 0f, projectileEvent.PosY);
                            if (projectileEvent.LifeMs <= 0)
                            {
                                Object.Destroy(uGo);
                                live.Remove(projectileEvent.ProjectileId);
                            }
                        }
                        break;
                    }
                    case ProjectileAction.Despawn:
                    {
                        if (live.TryGetValue(projectileEvent.ProjectileId, out var dGo))
                        {
                            if (dGo) Object.Destroy(dGo);
                            live.Remove(projectileEvent.ProjectileId);
                        }
                        break;
                    }
                }
            }
        }
    }
}
